using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

using JG_Prospect.DAL.Database;
using JG_Prospect.Common;
using JG_Prospect.Common.modal;
using System.Xml;

namespace JG_Prospect.DAL
{
    public class readXML
    {

        public static readXML m_readXML = new readXML();
        private readXML()
        {
        }
        public static readXML Instance
        {
            get { return m_readXML; }
            private set { ;}
        }
        public DataSet BulkIntsallUser(string xmlDoc)
        {
            DataSet dsTemp = new DataSet();

            try
            {
                SqlDatabase database = MSSQLDataBase.Instance.GetDefaultDatabase();
                {
                    DbCommand command = database.GetStoredProcCommand("UDP_BulkInstallUser");
                    database.AddInParameter(command, "@XMLDOC2", SqlDbType.Xml, xmlDoc);
                    dsTemp = database.ExecuteDataSet(command);
                    return dsTemp;
                }
            }
            catch (Exception ex)
            {
                return dsTemp = null;
            }
            return dsTemp;
        }
    }
}
