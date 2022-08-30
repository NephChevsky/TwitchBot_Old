import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { environment } from '../../environments/environment';
import { interval, Subscription, timer } from 'rxjs';
import { map, switchMap } from 'rxjs/operators'

@Component({
	selector: 'app-buttons',
	templateUrl: './buttons.component.html',
	styleUrls: ['./buttons.component.css'],
})
export class ButtonsComponent implements OnInit
{

	public buttons: any[] = [];
	public subscription !: Subscription;

	constructor(public httpClient: HttpClient)
	{
		this.buttons = [{
			title: '',
			value: ''
		},
		{
			title: '',
			value: ''
		}];
	}

	ngOnInit(): void
	{
		this.subscription = timer(0, 15 * 1000).pipe(
			switchMap(() => this.httpClient.get(environment.baseUrl + "api/buttons"))
		).subscribe((data: any) =>
		{
			this.updateButton(1, data[0]);
			this.updateButton(2, data[1]);
		}, error =>
		{
			// TODO: handle error
		});
	}

	ngOnDestroy()
	{
		this.subscription.unsubscribe();
	}

	updateButton(index: number, data: any)
	{
		this.buttons[index - 1] = {
			title: data.title,
			value: data.value
		}
		var element = document.getElementById("button" + index);
		if (this.buttons[index - 1].value.length > 23)
		{
			element?.classList.add("scroll-text");
		}
		else
		{
			element?.classList.remove("scroll-text");
		}
	}
}
