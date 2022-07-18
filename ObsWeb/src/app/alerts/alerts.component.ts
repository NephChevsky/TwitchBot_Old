import { Component, OnInit } from '@angular/core';

@Component({
	selector: 'app-alerts',
	templateUrl: './alerts.component.html',
	styleUrls: ['./alerts.component.scss']
})
export class AlertsComponent implements OnInit
{

	constructor() { }

	ngOnInit(): void
	{

	}

	toggle(show: boolean = false)
	{
		var element = document.getElementById("container");
		if (show)
  			element?.classList.add("show-item");
		else
			element?.classList.remove("show-item");
	}

	async triggerAlert()
	{
		var audio = new Audio('../assets/alerts.wav');
		audio.play();
		this.toggle(true);
		setTimeout(this.toggle, 5000)
	}
}