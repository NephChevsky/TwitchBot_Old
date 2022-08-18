import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';


@Component({
	selector: 'app-stats',
	templateUrl: './stats.component.html',
	styleUrls: ['./stats.component.css']
})
export class StatsComponent implements OnInit
{
	dataSource: any = [];
	displayedColumns: string[] = ['position', 'name', 'presence', 'seen', 'uptimeTotal', 'uptimeMonth', 'uptimeDay', 'messageCountTotal', 'messageCountMonth', 'messageCountDay', 'bitsTotal', 'bitsMonth', 'bitsDay'];

	constructor(public http: HttpClient)
	{

	}

	ngOnInit(): void
	{
		this.http.get(environment.baseUrl + "Statistics").subscribe((data: any) =>
		{
			this.dataSource = data;
		}, error =>
		{
			// TODO: handle error
		});
	}

	ngOnDestroy()
	{

	}
}
