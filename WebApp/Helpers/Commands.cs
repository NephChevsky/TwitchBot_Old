using ChatDll;
using DbDll;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ModelsDll.Db;

namespace WebApp.Helpers
{
    public static class Commands
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
	}
}
