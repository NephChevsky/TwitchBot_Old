import { Component, NgZone, OnInit } from '@angular/core';
import { HubClient } from '../services/hub.service';

@Component({
  selector: 'app-buttons',
  templateUrl: './buttons.component.html',
  styleUrls: ['./buttons.component.css'],
  providers: [HubClient]
})
export class ButtonsComponent implements OnInit {

  public buttons: any[] = [];

  constructor(private ngZone: NgZone, public hubClient: HubClient) {
    this.buttons = [{
      title: '',
      value: ''
    },
    {
      title: '',
      value: ''
    }];
    this.hubClient.GetStartedHubConnection().then(hub => hub.on("UpdateButton", data => this.ngZone.run(() => this.updateButton(data))));
  }

  ngOnInit(): void {
  }

  ngOnDestroy() {
    this.hubClient.StopHubConnection();
  }

  updateButton(data: any) {
    this.buttons[data.index - 1] = {
      title: data.title,
      value: data.value
    }
    var element = document.getElementById("button" + data.index);
    if (this.buttons[data.index - 1].value.length > 23) {
      element?.classList.add("scroll-text");
    }
    else {
      element?.classList.remove("scroll-text");
    }
  }
}
