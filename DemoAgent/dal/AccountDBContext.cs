using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class AccountDBContext
    {
        public static AccountDBContext Instance = new AccountDBContext();
        public Account? getAccount(string username, string password)
        {
            try
            {
                var context = new DemoAgentContext();
                return context.Accounts.FirstOrDefault(a => a.Username == username && a.PasswordKey == password) as Account;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Account? getAccountByUsername(string username)
        {
            try
            {
                var context = new DemoAgentContext();
                Account? account = context.Accounts.FirstOrDefault(a => a.Username == username);
                if (account != null)
                {
                    return account;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public Account? getAccountByPrivateKey(string privateKey)
        {
            try
            {
                var context = new DemoAgentContext();
                Account? account = context.Accounts.FirstOrDefault(a => a.PrivateKey == privateKey);
                if (account != null)
                {
                    return account;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public void addAccount(Account newAccount)
        {
            try
            {
                var context = new DemoAgentContext();
                context.Accounts.Add(newAccount);
                context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
