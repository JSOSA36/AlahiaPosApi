using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Data
{
    public static  class Server
    {
        //public static readonly string PathConexion= "Server=macrobitscorporation.ctugvuzidnpv.us-east-1.rds.amazonaws.com,1433;DataBase=AlahiaPos;" +
        //"User Id=admin;Password=JoelAriel87";
        //https://www.msn.com/es-xl/noticias/other/estados-unidos-las-radicales-medidas-que-prometi%C3%B3-donald-trump-en-su-retorno-a-la-casa-blanca/ar-AA1wmIfJ
        //public static readonly string PathConexion = @"Data Source=184.174.33.45;Initial Catalog=AlahiaPos;User ID=sa;Password=JoelAriel8787@@";
        //DataSource = @"WIN-4NM1M1CPR6R\SQLEXPRESS",

        public static string GetConectionString()
        {
            //Build an SQL connection string  

            SqlConnectionStringBuilder sqlString = new SqlConnectionStringBuilder()
            {
                //DataSource = @"144.126.143.154\SQLEXPRESS",
                //DataSource = @"DESKTOP-HI2GLCG\SQLEXPRESS",
                //DataSource = @"DESKTOP-BDUBGRR\SQLEXPRESS",

                //InitialCatalog = "AlahiaPosFastFood",
                //InitialCatalog = "BarraErickDb",
                //InitialCatalog = "AlahiaBeautySalonProd",
                //UserID = "sa",         //Username  
                //Password = "JoelAriel8787", //Password  
                                              //UserID = "sa",         //Username  
                                              //Password = "JoelAriel8787@@",  //Password  
                                              //IntegratedSecurity=true
                 //IntegratedSecurity = true



            };



            return sqlString.ToString();



        }
    }
}
