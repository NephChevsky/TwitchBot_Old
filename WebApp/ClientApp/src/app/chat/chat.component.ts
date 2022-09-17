import { Component, ElementRef, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HubClient } from '../services/hub.service';

@Component({
  selector: 'app-chat',
	templateUrl: './chat.component.html',
	styleUrls: ['./chat.component.css'],
	providers: [HubClient]
})
export class ChatComponent implements OnInit, OnDestroy
{
	public messages: any[] = [];
	public height: number = 0;
	public observer?: ResizeObserver;

	constructor(private ngZone: NgZone, public hubClient: HubClient, private zone: NgZone)
	{
		this.hubClient.GetStartedHubConnection().then(hub => hub.on("ChatOverlay", data => this.ngZone.run(() => this.pushMessage(data))));
	}

	ngOnInit()
	{
		this.height = window.innerHeight;
		this.observer = new ResizeObserver(entries =>
		{
			this.zone.run(() =>
			{
				const tableHeight = entries[0].contentRect.height;
				if (tableHeight > this.height)
				{
					this.messages.shift();
				}
			});
		});
		this.observer.observe(document.getElementById("chatbox")!);
	}

	pushMessage(data: any)
	{
		var offset = 0;
		data.emotes = data.emotes.sort((a: any, b: any) => a.startIndex > b.startIndex ? 1 : -1);
		for (var emote of data.emotes)
		{
			debugger;
			var img = " <img src='" + emote.imageUrl + "' /> ";
			data.message = data.message.substring(0, emote.startIndex + offset) + img + data.message.substring(emote.endIndex + 1 + offset);
			offset += img.length - ((emote.endIndex + 1) - emote.startIndex);
		}
		this.messages.push(data);
	}

	getBadge(name: string)
	{
		var badge = "";
		switch (name)
		{
			case "broadcaster":
				""
		}
	}

	ngOnDestroy()
	{
		this.hubClient.StopHubConnection();
		this.observer!.unobserve(document.getElementById("chatbox")!);
	}
}
