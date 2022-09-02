import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { MatSort, Sort } from '@angular/material/sort';

@Component({
	selector: 'app-stats',
	templateUrl: './stats.component.html',
	styleUrls: ['./stats.component.css']
})

export class StatsComponent implements OnInit
{
	originalSource: any;
	dataSource: any;
	displayedColumns: string[] = ['position', 'name', 'presence', 'seen', 'uptime', 'messageCount', 'bits', 'subs', 'subGifts', 'firstFollowDateTime'];
	minDate: string = "0001-01-01T00:00:00";
	currentSortColumn: string = "";
	currentSortDirection: string = "";

	@ViewChild(MatSort) sort!: MatSort;

	constructor(public http: HttpClient)
	{
	}

	formatDate(date: any, onlyDate: boolean = false)
	{
		if (typeof (date) == "string")
		{
			date = new Date(date);
		}
		var result = date.getDate().toString().padStart(2, '0') + "/"
			+ (date.getMonth() + 1).toString().padStart(2, '0') + "/"
			+ date.getFullYear();
		if (!onlyDate)
		{
			result += " "
				+ date.getHours().toString().padStart(2, '0') + ":"
				+ date.getMinutes().toString().padStart(2, '0') + ":"
				+ date.getSeconds().toString().padStart(2, '0');
		}
		return result;
	}

	sortData(sort: Sort)
	{
		const newData = this.dataSource.slice();
		if (!sort.active || sort.direction === '')
		{
			this.dataSource = this.originalSource;
			return;
		}

		this.dataSource = newData.sort((a: any, b: any) =>
		{
			const isAsc = sort.direction === 'asc';
			switch (sort.active)
			{
				case "presence":
					a = isAsc ? a['firstPresence'] : a['lastPresence'];
					b = isAsc ? b['firstPresence'] : b['lastPresence'];
					return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
				case "firstFollowDateTime":
					a = new Date(a[sort.active]);
					b = new Date(b[sort.active]);
					return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
				case "uptime":
				case "messageCount":
				case "bits":
				case "subGifts":
					a = a[sort.active + "Total"];
					b = b[sort.active + "Total"];
					return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
				default:
					return (a[sort.active] < b[sort.active] ? -1 : 1) * (isAsc ? 1 : -1);
			}
		});
	}

	ngOnInit(): void
	{
		this.http.get(environment.baseUrl + "api/Statistics").subscribe((data: any) =>
		{
			this.originalSource = data;
			this.dataSource = data;
		}, error =>
		{
			// TODO: handle error
		});
	}

	ngAfterViewInit()
	{
		this.dataSource.sort = this.sort;
	}

	ngOnDestroy()
	{

	}
}
