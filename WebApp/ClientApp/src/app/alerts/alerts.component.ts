import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';
import { Component, NgZone, OnInit } from '@angular/core';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})
export class AlertsComponent implements OnInit {

  private hubConnection?: HubConnection;
  private started?: Promise<void>;
  public username: string = "";
  public message: string = "";
  public messageContinued: string = "";
  public value: number = -1;

  constructor(private ngZone: NgZone) {
    this.GetStartedHubConnection().then(hub => hub.on("TriggerAlert", data => this.ngZone.run(() => this.triggerAlert(data))));
  }

  ngOnInit(): void {
  }

  ngOnDestroy() {
    this.StopHubConnection();
  }

  GetHubConnection(): HubConnection {
    if (!this.hubConnection) {
      this.hubConnection = new HubConnectionBuilder()
        .withUrl("/hub")
        .withAutomaticReconnect()
        .build();
    }

    this.hubConnection.onreconnecting(this.onreconnecting);
    return this.hubConnection;
  }

  GetStartedHubConnection(): Promise<HubConnection> {
    return new Promise<HubConnection>((resolve, reject) => {
      this.StartHubConnection()
        .then(() => resolve(this.hubConnection!))
        .catch(reject);
    });
  }

  RestartHubConnection() {
    this.StopHubConnection();
    this.StartHubConnection();
  }

  StopHubConnection() {
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      this.hubConnection.stop();
      delete this.started;
      delete this.hubConnection;
    }
  }

  StartHubConnection(): Promise<void> {
    if (!this.started) {
      this.started = this.GetHubConnection().start();
    }
    return this.started;
  }

  private onreconnecting(error: any) {
    console.log(error);
    console.log("Connection lost, reconnecting...");
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
        this.messageContinued = ""
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
        this.messageContinued = "viewers"
        break;
    }

    var audio = new Audio('../assets/alerts.wav');
    audio.play();
    this.toggle(true);
    setTimeout(this.toggle, 5000);
  }

}
