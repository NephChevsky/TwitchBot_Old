import { Component, NgZone, OnInit } from '@angular/core';
import { HubClient } from '../services/hub.service';

@Component({
	selector: 'app-alerts',
	templateUrl: './alerts.component.html',
	styleUrls: ['./alerts.component.css'],
	providers: [HubClient]
})
export class AlertsComponent implements OnInit
{
	public currentAlert: Alert = new Alert();
	public alerts: Alert[] = [];
	public intervalID!: number;
	public locked: boolean = false;

	constructor(private ngZone: NgZone, public hubClient: HubClient)
	{
		this.hubClient.GetStartedHubConnection().then(hub => hub.on("TriggerAlert", data => this.ngZone.run(() => this.handleAlert(data))));
	}

	ngOnInit(): void
	{
		this.intervalID = window.setInterval(() =>
		{
			if (this.alerts.length > 0 && !this.locked)
			{
				this.locked = true;
				this.triggerAlert(this.alerts[0]);
			}
		}, 100);
	}

	ngOnDestroy()
	{
		window.clearTimeout(this.intervalID);
		this.hubClient.StopHubConnection();
	}

	handleAlert(alert: any)
	{
		console.log("Alert " + alert.type + " received");
		var newAlert = new Alert();
		newAlert.type = alert.type;
		if (alert.emotes && alert.emotes.length > 0)
		{
			var offset = 0;
			alert.emotes = alert.emotes.sort((a: any, b: any) => a.startIndex > b.startIndex ? 1 : -1);
			for (var emote of alert.emotes)
			{
				var img = " <img src='" + emote.imageUrl + "' /> ";
				alert.message = alert.message.substring(0, emote.startIndex + offset) + img + alert.message.substring(emote.endIndex + 1 + offset);
				offset += img.length - ((emote.endIndex + 1) - emote.startIndex);
			}
		}
		switch (alert.type)
		{
			case "channel.follow":
				newAlert.username = alert.username;
				newAlert.message = "a follow la chaine!";
				newAlert.value = -1;
				break;
			case "channel.subscribe":
				newAlert.username = alert.username;
				newAlert.message = "s'est subscribe à la chaine!"
				newAlert.value = -1;
				break;
			case "channel.subscription.gift":
				newAlert.username = alert.username;
				newAlert.message = "a laché";
				newAlert.value = alert.total;
				newAlert.messageContinued = "sub gift(s)!";
				break;
			case "channel.subscription.message":
				newAlert.username = alert.username;
				newAlert.message = "s'est subscribe à la chaine pour";
				newAlert.value = alert.durationMonths;
				newAlert.messageContinued = "mois!"
				newAlert.messageContinued1 = alert.message;
				break;
			case "channel.cheer":
				newAlert.username = alert.username;
				newAlert.message = "a laché";
				newAlert.value = alert.bits;
				newAlert.messageContinued = "bits!";
				newAlert.messageContinued1 = alert.message;
				break;
			case "channel.raid":
				newAlert.username = alert.username;
				newAlert.message = "a raid la chaine avec";
				newAlert.value = alert.viewers;
				newAlert.messageContinued = "viewers";
				break;
			case "channel.hype_train.begin":
				newAlert.message = "Le hype train a démarré!";
				break;
		}

		if (alert.tts)
		{
			newAlert.tts = alert.tts;
		}

		this.alerts.push(newAlert);
	}

	triggerAlert(alert: Alert)
	{
		var audio;
		if (alert.type == "channel.hype_train.begin")
		{
			audio = new Audio('../assets/hype_train.wav');
		}
		else
		{
			audio = new Audio('../assets/alerts.wav');
		}

		this.currentAlert = this.alerts[0];

		audio.play();
		var element = document.getElementById("container");
		element?.classList.add("show-item");

		setTimeout(() =>
		{
			var element = document.getElementById("container");
			element?.classList.add("hide-item");
			element?.classList.remove("show-item");
			setTimeout(() =>
			{
				var element = document.getElementById("container");
				element?.classList.remove("hide-item");
				this.alerts.shift();
				this.locked = false;
			}, 1 * 1000);
		}, 6 * 1000);
		
		if (alert.tts)
		{
			setTimeout(() =>
			{
				audio = new Audio("data:audio/wav;base64," + alert.tts);
				audio.play();
			}, 1 * 1000);
		}
	}
}

export class Alert
{
	public type: string = "";
	public username: string = "";
	public message: string = "";
	public value: number = -1;
	public messageContinued: string = "";
	public messageContinued1: string = "";
	public tts: string = "";
}
