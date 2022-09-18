using ApiDll;
using Microsoft.Extensions.Configuration;
using ModelsDll;
using ModelsDll.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;

namespace StreamDeck.Forms
{
	public partial class StartStream : Form
	{
		private Api _api;

		public StartStream(Api api)
		{
			_api = api;
			InitializeComponent();
		}

		private async void StartStream_Load(object sender, EventArgs e)
		{
			ChannelInformation channel = await _api.GetChannelInformations();
			TitleText.Text = channel.Title;
			GameTitleText.Text = channel.GameName;
		}

		private async void StartButton_Click(object sender, EventArgs e)
		{
			ModifyChannelInformationResponse response = await _api.ModifyChannelInformations(TitleText.Text, GameTitleText.Text);
			if (response != null)
			{
				this.DialogResult = DialogResult.OK;
			}
			else
			{
				GameTitleText.BackColor = Color.LightPink;
				await Task.Delay(1000).ConfigureAwait(false);
				GameTitleText.BackColor = Color.White;
			}
		}
	}
}
