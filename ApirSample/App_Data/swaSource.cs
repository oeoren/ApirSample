using System;
using System.Linq;
using System.Web.Http;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Diagnostics;
using NLog;
namespace ControllerLibrary
{
public class products 
 { 
 public int? ProductId { get; set;} 
 public string ProductName { get; set;} 
}
static public class DbUtil 

{
static public string message;
	static public SqlConnection GetConnection() {
		string conStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
		SqlConnection con = new SqlConnection(conStr);
DbUtil.message = "";
con.InfoMessage += delegate(object sender, SqlInfoMessageEventArgs e)
{
if (DbUtil.message.Length > 0) DbUtil.message +=  "\n";
DbUtil.message +=  e.Message;
};
		return con;        
	}
}

    public static class DbExtensions
    {
        public static List<T> ToListCollection<T>(this DataTable dt)
        {
            List<T> lst = new System.Collections.Generic.List<T>();
            Type tClass = typeof(T);
            PropertyInfo[] pClass = tClass.GetProperties();
            List<DataColumn> dc = dt.Columns.Cast<DataColumn>().ToList();
            T cn;
            foreach (DataRow item in dt.Rows)
            {
                cn = (T)Activator.CreateInstance(tClass);
                foreach (PropertyInfo pc in pClass)
                {
                    // Can comment try catch block. 
                    try
                    {
                        DataColumn d = dc.Find(c => c.ColumnName == pc.Name);
                        if (d != null && item[pc.Name] != DBNull.Value)
                            pc.SetValue(cn, item[pc.Name], null);
                    }
                    catch (Exception ex)
                    {
                       throw ex;
                    }
                }
                lst.Add(cn);
            }
            return lst;
        }
    }
        
public class productsController:ApiController 
{
	 private static Logger logger = LogManager.GetCurrentClassLogger();
   public HttpResponseMessage Delete( int? ID = null)
 	{
	SqlConnection con = DbUtil.GetConnection();
	SqlCommand com = new SqlCommand("API_products_delete",con);
	com.CommandType = CommandType.StoredProcedure;
	SqlParameter RetVal = com.Parameters.Add("RetVal", SqlDbType.Int);
	RetVal.Direction = ParameterDirection.ReturnValue;
	com.Parameters.Add("ID", SqlDbType.Int).Value = ID;
	con.Open();
	com.ExecuteNonQuery();
	logger.Info("products_delete:@ID={0}, return={1}",ID,  RetVal.Value );
	if (0 == (int) RetVal.Value)
		 RetVal.Value = 200;
	if (200 == (int) RetVal.Value || 201 == (int)RetVal.Value)  
	{
		var response = Request.CreateResponse((HttpStatusCode)RetVal.Value, "null");
		return response;
	}
	if (DbUtil.message.Length > 0) 
		return Request.CreateErrorResponse((HttpStatusCode)RetVal.Value, DbUtil.message);
	else
		return Request.CreateResponse((HttpStatusCode)RetVal.Value);
	}
   public IEnumerable<products> Get()
	{
	SqlConnection con = DbUtil.GetConnection();
	SqlCommand com = new SqlCommand("API_products_get",con);
	com.CommandType = CommandType.StoredProcedure;
	SqlParameter RetVal = com.Parameters.Add("RetVal", SqlDbType.Int);
	RetVal.Direction = ParameterDirection.ReturnValue;
	SqlDataAdapter da = new SqlDataAdapter(com);
	con.Open();
	DataSet ds = new DataSet();
	da.Fill(ds);
	da.Dispose();
	logger.Info("products_get:, return={0}", RetVal.Value );
	DataTable dt = ds.Tables[0];
	List<products> ret = dt.ToListCollection<products>();
	return ret.AsEnumerable<products>();
	}


   public products Get( int? ID )
	{
	SqlConnection con = DbUtil.GetConnection();
	SqlCommand com = new SqlCommand("API_products_get",con);
	com.CommandType = CommandType.StoredProcedure;
	SqlParameter RetVal = com.Parameters.Add("RetVal", SqlDbType.Int);
	RetVal.Direction = ParameterDirection.ReturnValue;
	com.Parameters.Add("ID", SqlDbType.Int).Value = ID;
	SqlDataAdapter da = new SqlDataAdapter(com);
	con.Open();
	DataSet ds = new DataSet();
	da.Fill(ds);
	da.Dispose();
	logger.Info("products_get:@ID={0}, return={1}",ID,  RetVal.Value );
	if ( ds.Tables.Count == 0)
		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
	DataTable dt = ds.Tables[0];
	if (dt.Rows.Count > 0) 
		return  dt.ToListCollection<products>()[0];
	else 
		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
	}


/// <summary>
/// Add a new product to the database
/// </summary>
/// <remarks> A link to the new product is added  </remarks>
/// <response code="201">OK</response>
/// <response code="521">Bad productname</response>
/// <response code="522">Product with name exists</response>
   public HttpResponseMessage  Post(products res )
 	{ 
	if (res == null) { 
		logger.Fatal("products_post: Cannot parse resource. Check parameters" );
		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
	}
	 int? NewId = null;
	SqlConnection con = DbUtil.GetConnection();
	SqlCommand com = new SqlCommand("API_products_post",con);
	com.CommandType = CommandType.StoredProcedure;
	SqlParameter RetVal = com.Parameters.Add("RetVal", SqlDbType.Int);
	RetVal.Direction = ParameterDirection.ReturnValue;
	com.Parameters.Add("ProductName", SqlDbType.VarChar, 100).Value = res.ProductName;
	com.Parameters.Add("NewId", SqlDbType.Int).Value = NewId;
	com.Parameters["NewId"].Direction = ParameterDirection.Output; 
	try {
	con.Open();
	com.ExecuteNonQuery();
	} catch (Exception ex) {
	logger.Info("products_post:@ProductName={0}, @NewId={1}, return={2}",res.ProductName,NewId,  RetVal.Value );
		logger.Fatal("products_post: SqlException:" + ex.Message  );
		 return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
	} 
	logger.Info("products_post:@ProductName={0}, @NewId={1}, return={2}",res.ProductName,NewId,  RetVal.Value );
	if (0 == (int) RetVal.Value)
		 RetVal.Value = 200;
	if (200 == (int) RetVal.Value || 201 == (int)RetVal.Value)  
	{
		var response = Request.CreateResponse((HttpStatusCode)RetVal.Value, "null");
		string uri=Url.Link("DefaultApi", new { id = com.Parameters["NewID"].Value.ToString() });
		response.Headers.Location = new Uri(uri);
		return response;
	}
	if (DbUtil.message.Length > 0) 
		return Request.CreateErrorResponse((HttpStatusCode)RetVal.Value, DbUtil.message);
	else
		return Request.CreateResponse((HttpStatusCode)RetVal.Value);
	}
   public HttpResponseMessage Put(products res ,  int? ID = null)
 	{
	SqlConnection con = DbUtil.GetConnection();
	SqlCommand com = new SqlCommand("API_products_put",con);
	com.CommandType = CommandType.StoredProcedure;
	SqlParameter RetVal = com.Parameters.Add("RetVal", SqlDbType.Int);
	RetVal.Direction = ParameterDirection.ReturnValue;
	com.Parameters.Add("ID", SqlDbType.Int).Value = ID;
	com.Parameters.Add("ProductName", SqlDbType.VarChar, 100).Value = res.ProductName;
	con.Open();
	com.ExecuteNonQuery();
	logger.Info("products_put:@ID={0}, @ProductName={1}, return={2}",ID,res.ProductName,  RetVal.Value );
	if (0 == (int) RetVal.Value)
		 RetVal.Value = 200;
	if (200 == (int) RetVal.Value || 201 == (int)RetVal.Value)  
	{
		var response = Request.CreateResponse((HttpStatusCode)RetVal.Value, "null");
		return response;
	}
	if (DbUtil.message.Length > 0) 
		return Request.CreateErrorResponse((HttpStatusCode)RetVal.Value, DbUtil.message);
	else
		return Request.CreateResponse((HttpStatusCode)RetVal.Value);
	}
}
}

