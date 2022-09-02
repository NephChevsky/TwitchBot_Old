import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MaterialModule } from './material.module';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { AlertsComponent } from './alerts/alerts.component';
import { ButtonsComponent } from './buttons/buttons.component';
import { CommandsListComponent } from './commands-list/commands-list.component';
import { StatsComponent } from './stats/stats.component';

@NgModule({
	declarations: [
		AppComponent,
		HomeComponent,
		AlertsComponent,
		ButtonsComponent,
		CommandsListComponent,
		StatsComponent
	],
	imports: [
		BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
		BrowserAnimationsModule,
		HttpClientModule,
		FormsModule,
		MaterialModule,
		RouterModule.forRoot([
			{ path: '', component: HomeComponent, pathMatch: 'full' },
			{ path: 'alerts', component: AlertsComponent },
			{ path: 'buttons', component: ButtonsComponent },
			{ path: 'commands-list', component: CommandsListComponent },
			{ path: 'stats', component: StatsComponent }
		])
	],
	providers: [],
	bootstrap: [AppComponent]
})
export class AppModule { }
