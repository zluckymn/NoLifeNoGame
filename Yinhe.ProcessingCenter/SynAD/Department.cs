using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 部门实体
    /// </summary>
    public class ADDepartment
    {
        public ADDepartment()
        {
            SubDepartemnt = new List<ADDepartment>();

            SubUsers = new List<ADUser>();
        }
        public int DepId { get; set; }

        public string Guid { get; set; }

        public string ParentGuid { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string ParentName { get; set; }

        public string GrandParentName { get; set; }

        public string Code { get; set; }

        public string Path { get; set; }

        public int Level { get; set; }

        public int NodeType { get; set; }

        public List<ADDepartment> SubDepartemnt;

        public List<ADUser> SubUsers;

        
    }
    /// <summary>
    /// AD同步用户实体
    /// </summary>
    public class ADUser {

        public int UserId { get; set; }

        public string Guid { get; set; }

        public string DepartMentID { get; set; }

        public string DepartMentGuid { get; set; }

        public string GrandDepartMentID { get; set; }

        public string Name { get; set; }

        public string GiveName { get; set; }

        public string Code { get; set; }

        public string LoginName{get;set;}

        public string PassWord { get; set; }

        public string PhoneNumber { get; set; }

        public string MobieNumber { get; set; }

        public string EmailAddr { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public int Status { get; set; }

        public List<string> DepartMentGuids { get; set; }

        public string Remark { get; set; }
    }
}
