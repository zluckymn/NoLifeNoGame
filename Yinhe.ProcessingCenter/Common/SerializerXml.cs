using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml.Serialization;
//using Yinhoo.Autolink.Business.Common;
namespace Yinhe.ProcessingCenter
{

   /// <summary>
   /// Xml序列号通用处理类
   /// </summary>
   /// <typeparam name="T"></typeparam>
    public class SerializerXml<T> where T:class
    {
        public T Entity { get; set; }
        public string Message { get; set; }
        public SerializerXml(T entity) {
            this.Entity = entity;
        }

        public static SerializerXml<T> _(T entity) {
            return new SerializerXml<T>(entity);
        }
        
        public bool BuildXml(string xmlFileName) {
            bool bResult = false;
            FileStream fsXml = null;
            try
            {

                fsXml = new FileStream(xmlFileName, FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
                
                StreamWriter swXml = new StreamWriter(fsXml);

                XmlSerializer serizer = new XmlSerializer(this.Entity.GetType());
                serizer.Serialize(swXml, this.Entity);
                swXml.Flush();
                swXml.Close();
                fsXml.Close();
                swXml.Dispose();
                fsXml.Dispose();
                bResult = true;
            }
            catch(Exception ex) {
                this.Message = ex.Message.ToString();
            }
            finally
            {
                if (fsXml != null)
                {
                    fsXml.Close();
                    fsXml.Dispose();
                }
            }
            return bResult;
        }

        public  T BuildObject(string xmlFileName) {
            if (!File.Exists(xmlFileName)) {
                this.Message = "序列化文件：" + xmlFileName + "  不存在！";
                return this.Entity; 
            }
            FileStream fsXml = new FileStream(xmlFileName, FileMode.Open, FileAccess.ReadWrite,FileShare.Read);
            XmlSerializer serizer = new XmlSerializer(this.Entity.GetType());
            try
            {
                this.Entity = serizer.Deserialize(fsXml) as T;
            }
            catch (Exception ex)
            {
                Yinhoo.Framework.Log.LogWarpper log = Yinhoo.Framework.Log.LogWarpper._();
                log.PushApplicationException(ex);
            }
            finally {
                fsXml.Close();
                fsXml.Dispose();
            }
 

            return this.Entity;
        }
    }
}
