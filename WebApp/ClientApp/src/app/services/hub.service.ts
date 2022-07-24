import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';
import { Injectable } from '@angular/core';

@Injectable()
export class HubClient {
  private hubConnection?: HubConnection;
  private started?: Promise<void>;

  constructor() {
  }

  public GetHubConnection(): HubConnection {
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

  public RestartHubConnection() {
    this.StopHubConnection();
    this.StartHubConnection();
  }

  public StopHubConnection() {
    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      this.hubConnection.stop();
      delete this.started;
      delete this.hubConnection;
    }
  }

  public StartHubConnection(): Promise<void> {
    if (!this.started) {
      this.started = this.GetHubConnection().start();
    }
    return this.started;
  }

  private onreconnecting(error: any) {
    console.log(error);
    console.log("Connection lost, reconnecting...");
  }
}
