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
	displayedColumns: string[] = ['position', 'name', 'presence', 'seen', 'uptime', 'messageCount', 'bits', 'subs', 'subGifts', 'firstFollowDateTime'];

	constructor(public http: HttpClient)
	{

	}

	formatDate(date: any)
	{
		if (typeof (date) == "string")
		{
			date = new Date(date);
		}
		return date.getDate().toString().padStart(2, '0') + "/"
			+ (date.getMonth() + 1).toString().padStart(2, '0') + "/"
			+ date.getFullYear() + " "
			+ date.getHours().toString().padStart(2, '0') + ":"
			+ date.getMinutes().toString().padStart(2, '0') + ":"
			+ date.getSeconds().toString().padStart(2, '0');
	}


	ngOnInit(): void
	{
		this.http.get(environment.baseUrl + "api/Statistics").subscribe((data: any) =>
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
