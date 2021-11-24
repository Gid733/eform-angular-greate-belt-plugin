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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using GreateBeltLocalizationService;
    using Infrastructure.Models.Report.Index;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eFormApi.BasePn.Abstractions;
    using Microting.eFormApi.BasePn.Infrastructure.Helpers;
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

                // var fieldIds = new List<int>();
                //
                // foreach (var eform in model.EformIds)
                // {
                //     var fieldId = sdkDbContext.Fields
                //                     .Where(x => eform + 1 == x.CheckListId)
                //                     .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                //                     .Select(x => x.Id)
                //                     .FirstOrDefault();
                //     fieldIds.Add(fieldId);
                // }

                //var currentLanguage = await _userService.GetCurrentUserLanguage();
                var nameFields = new List<string> { "Id", "Value", "DoneAtUserModifiable", "DoneAt" };

                var casesQuery = sdkDbContext.Cases
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => model.EformIds.Contains(x.CheckListId.Value));

                if (model.Sort == "Name" || model.Sort == "ItemName")
                {
                    casesQuery = QueryHelper.AddFilterToQuery(casesQuery, nameFields, model.NameFilter);
                }
                else
                {
                    casesQuery = QueryHelper.AddFilterAndSortToQuery(casesQuery, model, nameFields);
                }

                var total = await casesQuery.Select(x => x.Id).CountAsync();
                casesQuery = casesQuery
                    .Skip(model.Offset)
                    .Take(model.PageSize);

                var foundCases = await casesQuery
                    .Select(x => new
                    {
                        x.Id,
                        CustomField1 = x.FieldValue1,
                        DoneAtUserEditable = x.DoneAtUserModifiable,
                        DoneBy = x.SiteId == null ? "" : x.Site.Name,
                        IsArchieved = x.IsArchived,
                    })
                    .ToListAsync();

                var foundCaseIds = foundCases.Select(x => x.Id).ToList();

                //var allFieldValues = core.Advanced_FieldValueReadList(foundCaseIds, currentLanguage);

                // var foundPlanningCasesSiteInfo = await _itemsPlanningPnDbContext.PlanningCaseSites
                //     //.Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                //     .Where(x => model.EformIds.Contains(x.MicrotingSdkeFormId))
                //     .Where(x => foundCaseIds.Contains(x.MicrotingSdkCaseId) || foundCaseIds.Contains(x.MicrotingCheckListSitId))
                //     .Select(x => new
                //     {
                //         x.PlanningId,
                //         x.MicrotingSdkCaseId,
                //         x.MicrotingCheckListSitId
                //     })
                //     .ToListAsync();

                var plannings = await _itemsPlanningPnDbContext.Plannings
                    .Where(x => model.EformIds.Contains(x.RelatedEFormId))
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Select(x => new
                    {
                        x.Id,
                        Name = x.NameTranslations
                            .Where(y => y.LanguageId == 1) //Danish
                            .Select(y => y.Name)
                            .FirstOrDefault(),
                    })
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
                        })
                    .ToList();

                var result = new Paged<GreateBeltReportIndexModel>
                {
                    Total = total,
                    Entities = foundCases
                                    .Select(x => new GreateBeltReportIndexModel
                                    {
                                        Id = x.Id,
                                        CustomField1 = x.CustomField1,
                                        DoneAtUserEditable = x.DoneAtUserEditable,
                                        DoneBy = x.DoneBy,
                                        ItemName = joined
                                            .Where(y => y.MicrotingSdkCaseId == x.Id)
                                            .Select(y => y.Name)
                                            .FirstOrDefault(),
                                        IsArchived = x.IsArchieved,
                                    })
                                    .ToList()
                };


                switch (model.Sort)
                {
                    case "Name":
                        {
                            if (model.IsSortDsc)
                            {
                                result.Entities = result.Entities.OrderByDescending(x => x.DoneBy).ToList();
                            }
                            else
                            {
                                result.Entities = result.Entities.OrderBy(x => x.DoneBy).ToList();
                            }

                            break;
                        }

                    case "ItemName":
                        {
                            if (model.IsSortDsc)
                            {
                                result.Entities = result.Entities.OrderByDescending(x => x.ItemName).ToList();
                            }
                            else
                            {
                                result.Entities = result.Entities.OrderBy(x => x.ItemName).ToList();
                            }
                            break;
                        }

                    default:
                        {
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