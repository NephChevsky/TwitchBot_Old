import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AlertsComponent } from './alerts/alerts.component';

const routes: Routes = [
  {
		path: 'alerts', component: AlertsComponent
	},
  {
    path: '**', redirectTo: ''
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
