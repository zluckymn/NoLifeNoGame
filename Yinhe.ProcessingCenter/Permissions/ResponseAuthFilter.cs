using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 响应流过滤去（权限过滤）
    /// </summary>
    public class ResponseAuthFilter : Stream
    {
        #region 私有变量
        private Stream responseStream;
        private long position;
        private StringBuilder html = new StringBuilder();
        private int UserId;
        #endregion

        #region 构造函数
        public ResponseAuthFilter(Stream inputStream, int userId)
        {
            responseStream = inputStream;
            this.UserId = userId;
        }

        #endregion

        #region implemented abstract members

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            responseStream.Flush();
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return position; }
            set { position = value; }
        }

        public override long Seek(long offset, System.IO.SeekOrigin direction)
        {
            return responseStream.Seek(offset, direction);
        }

        public override void SetLength(long length)
        {
            responseStream.SetLength(length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return responseStream.Read(buffer, offset, count);
        }

        #endregion

        private List<UserRight> _AllUserRight;
        /// <summary>
        /// 用户权限代码列表
        /// </summary>
        private List<UserRight> AllUserRight
        { 
            get{
               if(_AllUserRight==null)
               {
                   this._AllUserRight = AuthManage._().GetUserFunctionRight(this.UserId);
                   if (this._AllUserRight == null) {
                       this._AllUserRight = new List<UserRight>();
                   }
               }
               return this._AllUserRight;
            }
        }

        private Dictionary<string, List<string>> _DicDataRigh;
        /// <summary>
        /// 权限项代码
        /// </summary>
        private Dictionary<string, List<string>> DicDataRigh
        {
            get{
                if(this._DicDataRigh==null)
                {
                    this._DicDataRigh = new Dictionary<string,List<string>>();
                }
                return this._DicDataRigh;
            }
        }

        #region write method

        StringBuilder sbHtml = new StringBuilder();
        /// <summary>
        /// 过滤输出的Html代码
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            string chunk = System.Text.UTF8Encoding.UTF8.GetString(buffer, offset, count);
            sbHtml.Append(chunk);
        }

        public override void Close()
        {
            string html = sbHtml.ToString();
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("dt");
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("dd");
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("dl");
            htmlDoc.LoadHtml(html);
            //查找需要权限控制的html标签
            HtmlAgilityPack.HtmlNodeCollection chkRightNodes = htmlDoc.DocumentNode.SelectNodes("//@" + AuthManage.CHECKRIGHTAG);

            if (chkRightNodes == null || chkRightNodes.Count() == 0)
            {
                byte[] originalData = System.Text.UTF8Encoding.UTF8.GetBytes(html);
                responseStream.Write(originalData, 0, originalData.Length);
                htmlDoc = null;
                base.Close();
                responseStream.Close();
                return;
            }
            //用户权限代码列表
            List<UserRight> sysUserRights = this.AllUserRight.Where(u => u.DataId.HasValue == true && u.DataId.Value == 0).ToList();
            List<string> userRightCodes = sysUserRights.Select(r => r.Code).Distinct().ToList();

            foreach (var node in chkRightNodes)
            {
                if (node.Attributes.Contains(AuthManage.CHECKRIGHTAG) == false) { continue; }
                string rightCode = node.Attributes[AuthManage.CHECKRIGHTAG].Value;
                //多个权限 MODULECODE_RIGHTCODE|1|数据对象Id_数据实例Id,MODULECODE_RIGHTCODE
                //MODULECODE_RIGHTCODE|userId_1
                //MODULECODE_RIGHTCODE|数据对象Id_数据实例Id
                ////MODULECODE_RIGHTCODE|userId_1|数据对象Id_数据实例Id
                // MODULECODE_RIGHTCODE --权限代码
                //userId_1 --标示具有指定权限代码并且数据的创建人是本人
                //数据对象Id_数据实例Id --指定该权限项验证的是指定数据对象，数据实例的权限（比如某个项目的权限）
                string[] arrCode = rightCode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                bool hasRight = false;
                #region 判断是否有权限
                foreach (var c in arrCode)
                {
                    //格式， 1|MODULECODE_RIGHTCODE
                    string[] arrc = c.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    switch (arrc.Length)
                    {
                        case 1:
                            //只有系统权限项判断
                            if (userRightCodes.Contains(arrc[0]) == true)
                            {
                                hasRight = true;
                            }
                            break;
                        case 2:
                            //系统权限项+创建用户 或者 数据权限+指定数据对象+指定对象实例Id
                            string[] arrSecond = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                            if (arrSecond.Length == 2)
                            {
                                if (arrSecond[0] == AuthManage.USERTAG)
                                {
                                    //创建用户
                                    hasRight = userRightCodes.Contains(arrc[0]) == true && arrSecond[1] == this.UserId.ToString();
                                }
                                else
                                {
                                    hasRight = this.CheckDataRight(int.Parse(arrSecond[0]), int.Parse(arrSecond[1]), arrc[0], null);
                                }
                            }
                            break;
                        case 3:
                            //数据权限（项目）,并且配合创建人
                            //创建人
                            string[] arrUser = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                            //数据对象-数据实例
                            string[] arrData = arrc[2].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                            if (arrUser.Length == 2 && arrData.Length == 2)
                            {
                                hasRight = this.CheckDataRight(int.Parse(arrData[0]), int.Parse(arrData[1]), arrc[0], int.Parse(arrUser[1]));
                            }
                            break;
                    }
                    if (hasRight == true)
                    {
                        break;
                    }
                }
                if (hasRight == false)
                {
                    node.ParentNode.RemoveChild(node, false);
                }
                #endregion
            }
            //过滤后的html代码
            var sbFilter = new StringBuilder();
            using (var writer = new StringWriter(sbFilter))
            {
                htmlDoc.Save(writer);
            }
            byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(sbFilter.ToString());

            responseStream.Write(data, 0, data.Length);
            base.Close();
            responseStream.Close();
        }
        
        #endregion

        /// <summary>
        /// 检查数据权限代码
        /// </summary>
        /// <param name="dataObjId"></param>
        /// <param name="dataId"></param>
        /// <param name="code"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private bool CheckDataRight(int dataObjId,int dataId,string code,int? userId)
        {
            string dicKey = string.Format("{0}_{1}", dataObjId, dataId);
            List<string> dataUserRightCodes = new List<string>();
            if(this.DicDataRigh.ContainsKey(dicKey)==false)
            {
                dataUserRightCodes = this.AllUserRight
                    .Where(u=>u.DataObjId==dataObjId && u.DataId==dataId)
                    .Select(u=>u.Code)
                    .Distinct()
                    .ToList();
                this.DicDataRigh.Add(dicKey,dataUserRightCodes);
            }else{
               dataUserRightCodes = this.DicDataRigh[dicKey];
            }
            bool checkResult = checkResult = dataUserRightCodes.Contains(code);
            if(userId.HasValue==true)
            {
                checkResult = checkResult && userId.Value==this.UserId;
            }
            return false;
        }
    }
}
