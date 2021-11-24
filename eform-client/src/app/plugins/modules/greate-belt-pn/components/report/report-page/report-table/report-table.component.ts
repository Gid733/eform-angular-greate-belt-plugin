import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import { Paged, TableHeaderElementModel } from 'src/app/common/models';
import { GreateBeltPnClaims } from '../../../../enums';
import { ReportStateService } from '../../store';
import { ReportCaseModel } from '../../../../models';
import { STANDARD_DATE_FORMAT } from 'src/app/common/const';

@Component({
  selector: 'app-report-table',
  templateUrl: './report-table.component.html',
  styleUrls: ['./report-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportTableComponent implements OnInit {
  @Input() reportModel: Paged<ReportCaseModel> = new Paged<ReportCaseModel>();
  @Output()
  showArchiveCaseModal: EventEmitter<ReportCaseModel> = new EventEmitter<ReportCaseModel>();
  @Output()
  showRemoveCaseModal: EventEmitter<ReportCaseModel> = new EventEmitter<ReportCaseModel>();
  @Output()
  downloadPdf: EventEmitter<ReportCaseModel> = new EventEmitter<ReportCaseModel>();
  @Output()
  sortChange: EventEmitter<void> = new EventEmitter<void>();

  tableHeaders: TableHeaderElementModel[] = [
    { name: 'Id', elementId: 'idTableHeader', sortable: true },
    {
      name: 'FieldValue1',
      elementId: 'customField1TableHeader',
      sortable: true,
      visibleName: "Maximo arbejdsordre nr",
    },
    {
      name: 'DoneAtUserModifiable',
      elementId: 'doneAtTableHeader',
      sortable: true,
      visibleName: "Done at",
    },
    {
      name: 'Name',
      elementId: 'doneByTableHeader',
      sortable: true,
      visibleName: "Done by'",
    },
    { name: 'ItemName', elementId: 'itemPlanningNameTableHeader', sortable: true, visibleName: "Area" },
    { name: 'IsArchived', elementId: 'statusTableHeader', sortable: true, visibleName: "Status" },
    { name: 'Actions', elementId: '', sortable: false },
  ];

  get greateBeltPnClaims() {
    return GreateBeltPnClaims;
  }

  get dateFormat() {
    return STANDARD_DATE_FORMAT;
  }

  constructor() {}

  ngOnInit(): void {}

  onShowRemoveCaseModal(planning: ReportCaseModel) {
    this.showRemoveCaseModal.emit(planning);
  }

  onShowArchiveCaseModal(planning: ReportCaseModel) {
    this.showArchiveCaseModal.emit(planning);
  }

  onDownloadPdf(model: ReportCaseModel) {
    this.downloadPdf.emit(model);
  }

  sortTable(sort: string) {
    this.reportStateService.onSortTable(sort);
    this.sortChange.emit();
  }

}
