import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';


@Component({
	selector: 'app-commands-list',
	templateUrl: './commands-list.component.html',
	styleUrls: ['./commands-list.component.css']
})
export class CommandsListComponent implements OnInit
{

	public options: any = {};
	public commands: any = [];

	constructor(public http: HttpClient)
	{

	}

	ngOnInit(): void
	{
		this.http.get(environment.baseUrl + "api/Commands").subscribe((data: any) =>
		{
			this.commands = data.customCommands;
			delete data.customCommands;
			this.options = data;
		}, error =>
		{
			// TODO: handle error
		});
	}

	ngOnDestroy()
	{

	}
}
