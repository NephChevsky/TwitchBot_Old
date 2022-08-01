import { Component, NgZone, OnInit } from '@angular/core';
import { HubClient } from '../services/hub.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css'],
  providers: [HubClient]
})
export class AlertsComponent implements OnInit {

  public username: string = "";
  public message: string = "";
  public value: number = -1;
  public messageContinued: string = "";
  public messageContinued1: string = "";

  constructor(private ngZone: NgZone, public hubClient: HubClient) {
    this.hubClient.GetStartedHubConnection().then(hub => hub.on("TriggerAlert", data => this.ngZone.run(() => this.triggerAlert(data))));
  }

  ngOnInit(): void {
  }

  ngOnDestroy() {
    this.hubClient.StopHubConnection();
  }

  toggle(show: boolean = false) {
    var element = document.getElementById("container");
    if (show)
      element?.classList.add("show-item");
    else
      element?.classList.remove("show-item");
  }

  async triggerAlert(alert: any) {
    switch (alert.type) {
      case "channel_follow":
        this.username = alert.username;
        this.message = "a follow la chaine!";
        this.value = -1;
        this.messageContinued = "";
        this.messageContinued1 = "";
        break;
      case "channel.subscribe":
        this.username = alert.username;
        this.message = "s'est subscribe à la chaine!"
        this.value = -1;
        this.messageContinued = "";
        this.messageContinued1 = "";
        break;
      case "channel.subscription.gift":
        this.username = alert.username;
        this.message = "a laché";
        this.value = alert.total;
        this.messageContinued = "sub gift(s)!";
        this.messageContinued1 = "";
        break;
      case "channel.subscription.message":
        this.username = alert.username;
        this.message = "s'est subscribe à la chaine pour";
        this.value = alert.durationMonths;
        this.messageContinued = "mois!"
        this.messageContinued1 = alert.message;
        break;
      case "channel.cheer":
        this.username = alert.username;
        this.message = "a laché";
        this.value = alert.bits;
        this.messageContinued = "bits!";
        this.messageContinued1 = alert.message;
        break;
      case "channel.raid":
        this.username = alert.username;
        this.message = "a raid la chaine avec";
        this.value = alert.viewers;
        this.messageContinued = "viewers";
        this.messageContinued1 = "";
        break;
      case "channel.hype_train.begin":
        this.username = "";
        this.message = "Le hype train a démarré!";
        this.value = -1;
        this.messageContinued = "";
        this.messageContinued1 = "";
        break;
    }

    var audio;
    if (alert.type == "channel.hype_train.begin")
    {
      audio = new Audio('../assets/hype_train.wav');
    }
    else
    {
      audio = new Audio('../assets/alerts.wav');
    }
    audio.play();
    this.toggle(true);
    setTimeout(this.toggle, 5000);
  }

}
