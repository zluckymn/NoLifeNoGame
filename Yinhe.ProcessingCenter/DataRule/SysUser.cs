using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.ComponentModel;

namespace ProcessingCenter.RuleEntity
{
    public class SysUser : BsonDocument
    {
        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);

        public int userId
        {
            get
            {
                return this.Int("userId");
            }
            set
            {
                if ((this.Int("userId") != value))
                {
                    this.SetElement(new BsonElement("userId", value.ToString()));
                }
            }
        }

        public string name
        {
            get
            {
                return this.String("name");
            }
            set
            {
                if ((this.String("name") != value))
                {
                    this.SetElement(new BsonElement("name", value.ToString()));
                }
            }
        }

        public string loginName
        {
            get
            {
                return this.String("loginName");
            }
            set
            {
                if ((this.String("loginName") != value))
                {
                    this.SetElement(new BsonElement("loginName", value.ToString()));
                }
            }
        }

        public string loginPwd
        {
            get
            {
                return this.String("loginPwd");
            }
            set
            {
                if ((this.String("loginPwd") != value))
                {
                    this.SetElement(new BsonElement("loginPwd", value.ToString()));
                }
            }
        }

        public DateTime createDate
        {
            get
            {
                return this.Date("createDate");
            }
            set
            {
                if ((this.Date("createDate") != value))
                {
                    this.SetElement(new BsonElement("createDate", value.ToString("yyyy-MM-dd HH:mm:ss")));
                }
            }
        }

    }
}
