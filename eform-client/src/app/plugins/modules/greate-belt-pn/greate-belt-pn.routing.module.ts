import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReportContainerComponent } from './components';
import { GreateBeltPnLayoutComponent } from './layouts';

export const routes: Routes = [
  {
    path: '',
    component: GreateBeltPnLayoutComponent,
    children: [
      {
        path: 'report/storebaelt/14-dags',
        component: ReportContainerComponent,
      },
      {
        path: 'report/storebaelt/2-mdr',
        component: ReportContainerComponent,
      },
      {
        path: 'report/storebaelt/tr',
        component: ReportContainerComponent,
      },
      {
        path: 'report/storebaelt/saekcsltf',
        component: ReportContainerComponent,
      },
      {
        path: 'report/storebaelt/sse',
        component: ReportContainerComponent,
      },
      {
        path: 'report/oresund/14-dags',
        component: ReportContainerComponent,
      },
      {
        path: 'report/oresund/tr',
        component: ReportContainerComponent,
      },
      {
        path: 'report/oresund/oesaekcsltf',
        component: ReportContainerComponent,
      },
      {
        path: 'report/oresund/oesse',
        component: ReportContainerComponent,
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class GreateBeltPnRouting {}
