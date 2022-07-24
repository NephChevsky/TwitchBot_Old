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
  public messageContinued: string = "";
  public value: number = -1;

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
        this.message = "a follow la chaine";
        this.messageContinued = "";
        break;
      case "channel.subscribe":
        break;
      case "channel.subscription.gift":
        break;
      case "channel.subscription.message":
        break;
      case "channel.cheer":
        break;
      case "channel.raid":
        this.username = alert.username;
        this.message = "a raid la chaine avec";
        this.value = alert.viewers;
        this.messageContinued = "viewers";
        break;
    }

    var audio = new Audio('../assets/alerts.wav');
    audio.play();
    this.toggle(true);
    setTimeout(this.toggle, 5000);
  }

}
