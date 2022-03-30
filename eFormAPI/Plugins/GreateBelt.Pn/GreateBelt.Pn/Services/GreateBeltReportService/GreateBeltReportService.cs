/*
The MIT License (MIT)
Copyright (c) 2007 - 2021 Microting A/S
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace GreateBelt.Pn.Services.GreateBeltReportService
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using GreateBeltLocalizationService;
    using Infrastructure.Models.Report.Index;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eFormApi.BasePn.Abstractions;
    using Microting.eFormApi.BasePn.Infrastructure.Models.API;
    using Microting.eFormApi.BasePn.Infrastructure.Models.Common;
    using Microting.ItemsPlanningBase.Infrastructure.Data;

    public class GreateBeltReportService : IGreateBeltReportService
    {
        private readonly ILogger<GreateBeltReportService> _logger;
        private readonly ItemsPlanningPnDbContext _itemsPlanningPnDbContext;
        private readonly IUserService _userService;
        private readonly IGreateBeltLocalizationService _localizationService;
        private readonly IEFormCoreService _core;

        public GreateBeltReportService(
            ILogger<GreateBeltReportService> logger,
            ItemsPlanningPnDbContext itemsPlanningPnDbContext,
            IUserService userService,
            IGreateBeltLocalizationService localizationService,
            IEFormCoreService core)
        {
            _logger = logger;
            _itemsPlanningPnDbContext = itemsPlanningPnDbContext;
            _userService = userService;
            _localizationService = localizationService;
            _core = core;
        }

        public async Task<OperationDataResult<Paged<GreateBeltReportIndexModel>>> Index(GreateBeltReportIndexRequestModel model)
        {
            try
            {
                var core = await _core.GetCore();
                var sdkDbContext = core.DbContextHelper.GetDbContext();

                var casesQuery = sdkDbContext.Cases
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.DoneAt != null)
                    .Where(x => model.EformIds.Contains(x.CheckListId.Value));

                var foundCases = await casesQuery
                    .Select(x => new
                    {
                        x.Id,
                        CustomField1 = x.FieldValue1,
                        DoneAtUserEditable = x.DoneAtUserModifiable,
                        DoneBy = x.SiteId == null ? "" : x.Site.Name,
                        x.CheckListId,
                        IsArchieved = x.IsArchived,
                    })
                    .ToListAsync();

                var foundCaseIds = foundCases.Select(x => x.Id).ToList();
                var planningQuery = _itemsPlanningPnDbContext.Plannings
                    .Where(x => model.EformIds.Contains(x.RelatedEFormId))
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Select(x => new
                    {
                        x.Id,
                        x.RelatedEFormId,
                        Name = x.NameTranslations
                            .Where(y => y.LanguageId == 1) //Danish
                            .Select(y => y.Name)
                            .FirstOrDefault(),
                    });

                var plannings = await planningQuery
                    .ToListAsync();

                var planningCasesQuery = _itemsPlanningPnDbContext.PlanningCases
                    .Include(x => x.Planning)
                    .Where(x => foundCaseIds.Contains(x.MicrotingSdkCaseId))
                    .Where(x => x.Status == 100)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .AsQueryable();

                var joined = plannings
                    .Join(planningCasesQuery, arg => arg.Id, arg => arg.PlanningId,
                        (x, y) => new
                        {
                            x.Name,
                            y.MicrotingSdkCaseId,
                            y.PlanningId,
                            y.MicrotingSdkeFormId
                        })
                    .ToList();

                var foundResultQuery = foundCases
                    .Select(x => new GreateBeltReportIndexModel
                    {
                        Id = x.Id,
                        CustomField1 = x.CustomField1 ?? "",
                        DoneAtUserEditable = x.DoneAtUserEditable,
                        DoneBy = x.DoneBy,
                        ItemName = joined
                            .Where(y => y.MicrotingSdkCaseId == x.Id)
                            .Select(y => y.Name)
                            .LastOrDefault(),
                        IsArchived = x.IsArchieved,
                        ItemId = joined.Where(y => y.MicrotingSdkCaseId == x.Id).Select(y => y.PlanningId).LastOrDefault(),
                        TemplateId = joined.Where(y => y.MicrotingSdkCaseId == x.Id).Select(y => y.MicrotingSdkeFormId).LastOrDefault()
                    });

                foundResultQuery = foundResultQuery
                    .Where(x => x.Id.ToString().Contains(model.NameFilter)
                    || x.CustomField1.ToLower().Contains(model.NameFilter)
                    || x.DoneAtUserEditable.ToString().Contains(model.NameFilter)
                    || x.DoneBy.ToLower().Contains(model.NameFilter)
                    || x.ItemName.ToLower().Contains(model.NameFilter))
                    .Select(x => x)
                    .ToList();

                var total = foundResultQuery.Select(x => x.Id).Count();
                foundResultQuery = foundResultQuery
                    .Skip(model.Offset)
                    .Take(model.PageSize);

                var result = new Paged<GreateBeltReportIndexModel>
                {
                    Total = total,
                    Entities = foundResultQuery.ToList()
                };


                switch (model.Sort)
                {
                    case "Name":
                    {
                        result.Entities = model.IsSortDsc
                            ? result.Entities.OrderByDescending(x => x.DoneBy).ToList()
                            : result.Entities.OrderBy(x => x.DoneBy).ToList();

                        break;
                    }
                    case "ItemName":
                    {
                        result.Entities = model.IsSortDsc
                            ? result.Entities.OrderByDescending(x => x.ItemName).ToList()
                            : result.Entities.OrderBy(x => x.ItemName).ToList();
                        break;
                    }
                    case "Id":
                    {
                        result.Entities = model.IsSortDsc
                            ? result.Entities.OrderByDescending(x => x.Id).ToList()
                            : result.Entities.OrderBy(x => x.Id).ToList();
                        break;
                    }
                    case "FieldValue1":
                    {
                        result.Entities = model.IsSortDsc
                            ? result.Entities.OrderByDescending(x => x.CustomField1).ToList()
                            : result.Entities.OrderBy(x => x.CustomField1).ToList();
                        break;
                    }
                    case "DoneAtUserModifiable":
                    {
                        result.Entities = model.IsSortDsc
                            ? result.Entities.OrderByDescending(x => x.DoneAtUserEditable).ToList()
                            : result.Entities.OrderBy(x => x.DoneAtUserEditable).ToList();
                        break;
                    }
                }

                return new OperationDataResult<Paged<GreateBeltReportIndexModel>>(true, result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"User {_userService.GetCurrentUserFullName()} logged in from GreateBeltReportService.Index");
                return new OperationDataResult<Paged<GreateBeltReportIndexModel>>(false,
                    _localizationService.GetString("ErrorWhileReadCases"));
            }
        }
    }
}