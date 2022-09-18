using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ModelsDll.Db;
using SpotifyDll;
using TwitchLib.Api.Core.Enums;

namespace HelpersDll
{
	public static class Helpers
	{
		public static bool AddCommand(BasicChat chat, string commandName, string commandMessage, string userId, string displayName)
		{
			Command cmd = new();
			cmd.Name = commandName;
			cmd.Message = commandMessage;
			cmd.Owner = userId;
			using (TwitchDbContext db = new())
			{
				db.Commands.Add(cmd);
				try
				{
					db.SaveChanges();
				}
				catch (DbUpdateException ex)
				when ((ex.InnerException as SqlException)?.Number == 2601 || (ex.InnerException as SqlException)?.Number == 2627)
				{
					chat.SendMessage($"{displayName} : Une commande du même nom existe déja");
					return false;
				}
				chat.SendMessage($"{displayName} : Command {commandName} créée");
				return true;
			}
		}

		public static bool DeleteCommand(BasicChat chat, string commandName, string displayName)
		{
			using (TwitchDbContext db = new())
			{
				Command dbCmd = db.Commands.Where(x => x.Name == commandName).FirstOrDefault();
				if (dbCmd != null)
				{
					db.Remove(dbCmd);
					db.SaveChanges();
					chat.SendMessage($"{displayName} : Command {commandName} supprimée");
					return true;
				}
				else
				{
					chat.SendMessage($"{displayName} : Commande inconnue");
					return false;
				}
			}
		}

		public static async Task<bool> SkipSong(Spotify spotify, BasicChat chat, string displayName)
		{
			if (!(await spotify.SkipSong()))
			{
				chat.SendMessage($"{displayName} : On écoute pas de musique bouffon");
				return false;
			}
			return true;
		}

		public static async Task<int> AddSong(Spotify spotify, BasicChat chat, string song, string displayName)
		{
			int ret = await spotify.AddSong(song);
			if (ret == 1)
			{
				chat.SendMessage($"{displayName} : La musique a été ajoutée à la playlist");
			}
			else if (ret == 2)
			{
				chat.SendMessage($"{displayName} : La musique est déjà dans la playlist");
			}
			else
			{
				chat.SendMessage($"{displayName} : La musique n'a pas pu être ajoutée à la playlist");
			}
			return ret;
		}

		public static async Task<bool> RemoveSong(Spotify spotify, BasicChat chat, string displayName)
		{
			bool ret = await spotify.RemoveSong();
			if (ret)
			{
				chat.SendMessage($"{displayName} : La musique a été supprimée de la playlist");
			}
			else
			{
				chat.SendMessage($"{displayName} : La musique n'a pas pu être supprimée de la playlist");
			}
			return ret;
		}
	}
}