import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { saveAs } from 'file-saver';
import { AutoUnsubscribe } from 'ngx-auto-unsubscribe';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { CaseModel, Paged } from 'src/app/common/models';
import {
  CaseArchiveModalComponent,
  CaseRemoveModalComponent,
} from 'src/app/common/modules/eform-cases/components';
import { EFormService } from 'src/app/common/services';
import { AuthStateService } from 'src/app/common/store';
import { ReportCaseModel } from 'src/app/plugins/modules/greate-belt-pn/models';
import { GreateBeltPnClaims } from '../../../../enums';
import { GreateBeltPnReportService } from '../../../../services';
import { ReportStateService } from '../../store';

@AutoUnsubscribe()
@Component({
  selector: 'app-report-container',
  templateUrl: './report-container.component.html',
  styleUrls: ['./report-container.component.scss'],
})
export class ReportContainerComponent implements OnInit, OnDestroy {
  @ViewChild('caseRemoveModal', { static: true })
  caseRemoveModal: CaseRemoveModalComponent;
  @ViewChild('caseArchiveModal', { static: true })
  caseArchiveModal: CaseArchiveModalComponent;
  nameSearchSubject = new Subject();
  reportModel: Paged<ReportCaseModel> = new Paged<ReportCaseModel>();
  selectedEformIds: number[] = [];

  getReportSub$: Subscription;
  downloadEformPdf$: Subscription;
  routeSub$: Subscription;

  constructor(
    private reportService: GreateBeltPnReportService,
    public reportStateService: ReportStateService,
    public authStateService: AuthStateService,
    private eFormService: EFormService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.nameSearchSubject.pipe(debounceTime(500)).subscribe((val) => {
      this.reportStateService.updateNameFilter(val.toString());
      this.getReport();
    });

    if (this.router.url.indexOf('/oresund/14-dags') > -1) {
      this.selectedEformIds = [11];
    }

    if (this.router.url.indexOf('/oresund/tr') > -1) {
      this.selectedEformIds = [449];
    }

    if (this.router.url.indexOf('/oresund/oesaekcsltf') > -1) {
      this.selectedEformIds = [451];
    }

    if (this.router.url.indexOf('/oresund/oesse') > -1) {
      this.selectedEformIds = [449];
    }

    if (this.router.url.indexOf('/storebaelt/2-mdr') > -1) {
      this.selectedEformIds = [23];
    }

    if (this.router.url.indexOf('/storebaelt/tr') > -1) {
      this.selectedEformIds = [36];
    }

    if (this.router.url.indexOf('/storebaelt/14-dags') > -1) {
      this.selectedEformIds = [11];
    }

    if (this.router.url.indexOf('/storebaelt/saekcsltf') > -1) {
      this.selectedEformIds = [38];
    }

    if (this.router.url.indexOf('/storebaelt/sse') > -1) {
      this.selectedEformIds = [40, 147, 191, 305, 412];
    }

    this.getReport();
  }

  ngOnInit() {}

  getReport() {
    this.getReportSub$ = this.reportStateService
      .getReport(this.selectedEformIds)
      .subscribe((data) => {
        if (data && data.success) {
          this.reportModel = data.model;
        }
      });
  }

  ngOnDestroy(): void {}

  onPageSizeChanged(newPageSize: number) {
    this.reportStateService.updatePageSize(newPageSize);
    this.getReport();
  }

  sortTable(sort: string) {
    this.reportStateService.onSortTable(sort);
    this.getReport();
  }

  changePage(newPage: any) {
    this.reportStateService.changePage(newPage);
    this.getReport();
  }

  onNameFilterChanged(name: string) {
    this.nameSearchSubject.next(name);
  }

  showRemoveCaseModal(model: ReportCaseModel) {
    this.caseRemoveModal.show(
      new CaseModel({
        id: model.id,
        doneAt: model.doneAtUserEditable,
        workerName: model.doneBy,
      }),
      model.templateId
    );
  }

  showArchiveCaseModal(model: ReportCaseModel) {
    this.caseArchiveModal.show({
      id: model.id,
      doneAt: model.doneAtUserEditable,
      templateId: model.templateId,
      workerName: model.doneBy,
    });
  }

  onDownloadPdf(model: ReportCaseModel) {
    this.downloadEformPdf$ = this.eFormService
      .downloadEformPDF(model.templateId, model.id, 'pdf')
      .subscribe((data) => {
        const blob = new Blob([data]);
        saveAs(blob, `template_${model.templateId}.pdf`);
      });
  }
}
