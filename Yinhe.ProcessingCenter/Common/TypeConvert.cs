using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Collections;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 类型转换处理类
    /// </summary>
    static public class TypeConvert
    {
        /// <summary>
        /// 将字符串转换为BsonDocument
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static BsonDocument ParamStrToBsonDocument(string sourceStr)
        {
            BsonDocument bsonDoc = new BsonDocument();

            if (sourceStr.Contains('&'))
            {
                sourceStr = sourceStr.Replace("&", " &");
            }
            else
            {
                sourceStr = sourceStr + " ";
            }

            // 开始分析参数对    
            Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);
            MatchCollection mc = re.Matches(sourceStr);

            foreach (Match m in mc)
            {
                bsonDoc.Add(m.Result("$2"), HttpUtility.UrlDecode(m.Result("$3")).Trim());
            }

            return bsonDoc;
        }

        /// <summary>
        /// 将键值对转换为BsonDocument
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static BsonDocument DicToBsonDocument(Dictionary<string, string> valueDic)
        {
            BsonDocument bsonDoc = new BsonDocument();
            bsonDoc.Add(valueDic);
            return bsonDoc;
        }

        /// <summary>
        /// 将NameValue结构转化为BsonDocument
        /// </summary>
        /// <param name="sourceNVC"></param>
        /// <returns></returns>
        public static BsonDocument NameValueToBsonDocument(NameValueCollection sourceNVC)
        {
            BsonDocument doc = new BsonDocument();

            foreach (var temp in sourceNVC.AllKeys)
            {
                if (temp != null)
                {
                    doc.Add(temp, sourceNVC[temp]);
                }
            }

            return doc;
        }

        /// <summary>
        /// 将字符串数据转化为NameValueCollection结构
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static NameValueCollection ParamStrToNameValue(string sourceStr)
        {
            NameValueCollection nvcData = new NameValueCollection();

            // 开始分析参数对    
            Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);
            MatchCollection mc = re.Matches(sourceStr);

            foreach (Match m in mc)
            {
                nvcData.Add(m.Result("$2"), HttpUtility.UrlDecode(m.Result("$3")));
            }

            return nvcData;
        }

        /// <summary>
        /// 将NameValue结构转化为Mongo查询类型
        /// </summary>
        /// <param name="sourceNVC"></param>
        /// <returns></returns>
        public static IMongoQuery NameValueToQuery(NameValueCollection sourceNVC)
        {
            var query = Query.Null;

            foreach (var tempKey in sourceNVC.AllKeys)
            {
                query = Query.And(query, Query.EQ(tempKey, sourceNVC[tempKey]));
            }

            return query;
        }

        /// <summary>
        /// 将字符串转化为Mongo查询类型
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public static IMongoQuery ParamStrToQuery(string sourceStr)
        {
            NameValueCollection nvcData = ParamStrToNameValue(sourceStr);

            return NameValueToQuery(nvcData);
        }

        /// <summary>
        /// 将原生查询语句转换为Query
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public static IMongoQuery NativeQueryToQuery(string queryStr)
        {
            var retQuery = Query.Null;

            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] tempArray = queryStr.Split(new string[] { "." }, StringSplitOptions.None);

                if (tempArray.Count() >= 3)
                {
                    MongoOperation mongoOp = new MongoOperation();          //连接mongo数据库

                    BsonValue tempVal = mongoOp.EvalNativeQuery(queryStr);

                    if (tempVal != null)
                    {
                        if (tempArray[2].StartsWith("distinct("))    //原生distinct查询,返回的是多条记录的In查询
                        {
                            retQuery = Query.In("_id", tempVal.AsBsonArray);
                        }
                        else if (tempArray[2].StartsWith("findOne("))   //原生findOne查询,返回的是单条记录的EQ查询
                        {
                            retQuery = Query.EQ("_id", tempVal.AsBsonDocument["_id"]);
                        }
                    }
                }
            }

            return retQuery;
        }

        /// <summary>
        /// 将InvokeResult转换为PageJson
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static PageJson InvokeResultToPageJson(InvokeResult result)
        {
            PageJson json = new PageJson();

            json.Success = result.Status == Status.Successful;

            if (result.Status == Status.Successful && result.BsonInfo != null)
            {
                json.htInfo = result.BsonInfo.ToHashtable();
            }

            json.Message = result.Message;
            json.FileInfo = result.FileInfo;
            return json;
        }

        /// <summary>
        /// 中文字符串转化为拼音
        /// </summary>
        /// <param name="cnStr"></param>
        /// <returns></returns>
        public static string CNStringToPinYinString(string cnStr)
        {
            byte[] array = new byte[2];
            array = System.Text.Encoding.Default.GetBytes(cnStr);
            int i = (short)(array[0] - '\0') * 256 + ((short)(array[1] - '\0'));

            if (i < 0xB0A1) return "*";
            if (i < 0xB0C5) return "a";
            if (i < 0xB2C1) return "b";
            if (i < 0xB4EE) return "c";
            if (i < 0xB6EA) return "d";
            if (i < 0xB7A2) return "e";
            if (i < 0xB8C1) return "f";
            if (i < 0xB9FE) return "g";
            if (i < 0xBBF7) return "h";
            if (i < 0xBFA6) return "g";
            if (i < 0xC0AC) return "k";
            if (i < 0xC2E8) return "l";
            if (i < 0xC4C3) return "m";
            if (i < 0xC5B6) return "n";
            if (i < 0xC5BE) return "o";
            if (i < 0xC6DA) return "p";
            if (i < 0xC8BB) return "q";
            if (i < 0xC8F6) return "r";
            if (i < 0xCBFA) return "s";
            if (i < 0xCDDA) return "t";
            if (i < 0xCEF4) return "w";
            if (i < 0xD1B9) return "x";
            if (i < 0xD4D1) return "y";
            if (i < 0xD7FA) return "z";

            return "*";
        }

        /// <summary>
        /// 将原生查询语句转换为结果字符串
        /// </summary>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public static string NativeQueryToResultValue(string queryStr)
        {
            string retStr = "";

            string[] tempArray = queryStr.Split(new string[] { "." }, StringSplitOptions.None);

            if (tempArray.Count() == 4 && tempArray[2].StartsWith("findOne("))
            {
                MongoOperation mongoOp = new MongoOperation();          //连接mongo数据库

                BsonValue tempVal = mongoOp.EvalNativeQuery(queryStr);

                retStr = tempVal != null ? tempVal.ToString() : "";
            }

            return retStr;
        }

        /// <summary>
        /// 将字符串列表转换为BsonValue列表
        /// </summary>
        /// <param name="strList"></param>
        /// <returns></returns>
        public static List<BsonValue> StringListToBsonValueList(IEnumerable<string> strList)
        {
            List<BsonValue> retList = new List<BsonValue>();

            foreach (var temp in strList)
            {
                retList.Add(temp);
            }

            return retList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static BsonValue ToObjectId(string _id)
        {
            ObjectId objId;
            ObjectId.TryParse(_id, out objId);
            return objId;

        }

        /// <summary>
        /// 将字符串列表转换成ObjectId列表
        /// </summary>
        /// <param name="strList">字符串列表</param>
        /// <returns></returns>
        public static List<BsonValue> ToObjectIdList(List<string> strList)
        {
            List<BsonValue> retList = new List<BsonValue>();

            foreach (var temp in strList)
            {
                ObjectId objId;
                if (ObjectId.TryParse(temp, out objId))
                {
                    retList.Add(objId);
                }
            }
            return retList;
        }

        /// <summary>
        /// 将spliter分隔的Id字符串解析
        /// </summary>
        /// <param name="Ids"></param>
        /// <param name="spliter">分隔符</param>
        /// <returns></returns>
        public static IEnumerable<int> StringToIntEnum(string Ids, string spliter)
        {
            string[] arr = Ids.Split(new string[] { spliter }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in arr)
            {
                int tmp = -1;
                int.TryParse(s, out tmp);

                if (tmp != -1)
                    yield return tmp;
            }
        }

        /// <summary>
        /// 将Bosn数据转化为日志格式字符串
        /// </summary>
        /// <param name="sourceBson"></param>
        /// <returns></returns>
        public static string BsonDocumentToLogStr(BsonDocument sourceBson)
        {
            List<string> filterList = new List<string>() { "_id", "underTable", "updateDate", "updateUserId" };     //多余字段

            BsonDocument tempData = new BsonDocument();

            if (BsonDocumentExtension.IsNullOrEmpty(sourceBson) == false)
            {
                foreach (var tempElement in sourceBson.Elements)
                {
                    if (filterList.Contains(tempElement.Name)) continue;

                    tempData.Add(tempElement.Name, tempElement.Value);
                }
            }

            return tempData.ToString();
        }

        /// <summary>
        /// 将Bosn数据转化为日志格式字符串
        /// </summary>
        /// <param name="sourceBson"></param>
        /// <returns></returns>
        public static string BsonDocumentToLogStr(List<BsonDocument> sourceList)
        {
            StringBuilder tempStr = new StringBuilder();

            if (sourceList != null && sourceList.Count > 0)
            {
                foreach (var tempSource in sourceList)
                {
                    tempStr.Append(BsonDocumentToLogStr(tempSource));
                }
            }
            return tempStr.ToString();
        }

        /// <summary>
        /// 金额小写转中文大写。
        /// 整数支持到万亿；小数部分支持到分(超过两位将进行Banker舍入法处理)
        /// </summary>
        /// <param name="Num">需要转换的双精度浮点数</param>
        /// <returns>转换后的字符串</returns>
        public static string MoneyLowerToUpper(Double Num)
        {

            String[] Ls_ShZ = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖", "拾" };
            String[] Ls_DW_Zh = { "元", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "万" };
            String[] Num_DW = { "", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "万" };
            String[] Ls_DW_X = { "角", "分" };
            
            Boolean iXSh_bool = false;//是否含有小数，默认没有(0则视为没有)
            Boolean iZhSh_bool = true;//是否含有整数,默认有(0则视为没有)

            string NumStr;//整个数字字符串
            string NumStr_Zh;//整数部分
            string NumSr_X = "";//小数部分
            string NumStr_DQ;//当前的数字字符
            string NumStr_R = "";//返回的字符串

            Num = Math.Round(Num, 2);//四舍五入取两位

            //各种非正常情况处理
            if (Num < 0)
                return string.Empty;
            if (Num > 9999999999999.99)
                return string.Empty;
            if (Num == 0)
                return Ls_ShZ[0];

            //判断是否有整数
            if (Num < 1.00)
                iZhSh_bool = false;

            NumStr = Num.ToString();

            NumStr_Zh = NumStr;//默认只有整数部分
            if (NumStr_Zh.Contains("."))
            {//分开整数与小数处理
                NumStr_Zh = NumStr.Substring(0, NumStr.IndexOf("."));
                NumSr_X = NumStr.Substring((NumStr.IndexOf(".") + 1), (NumStr.Length - NumStr.IndexOf(".") - 1));
                iXSh_bool = true;
            }


            if (NumSr_X == "" || int.Parse(NumSr_X) <= 0)
            {//判断是否含有小数部分
                iXSh_bool = false;
            }

            if (iZhSh_bool)
            {//整数部分处理
                NumStr_Zh = NumStr_Zh.Reverse();//反转字符串

                for (int a = 0; a < NumStr_Zh.Length; a++)
                {//整数部分转换
                    NumStr_DQ = NumStr_Zh.Substring(a, 1);
                    if (int.Parse(NumStr_DQ) != 0)
                        NumStr_R = Ls_ShZ[int.Parse(NumStr_DQ)] + Ls_DW_Zh[a] + NumStr_R;
                    else if (a == 0 || a == 4 || a == 8)
                    {
                        if (NumStr_Zh.Length > 8 && a == 4)
                            continue;
                        NumStr_R = Ls_DW_Zh[a] + NumStr_R;
                    }
                    else if (int.Parse(NumStr_Zh.Substring(a - 1, 1)) != 0)
                        NumStr_R = Ls_ShZ[int.Parse(NumStr_DQ)] + NumStr_R;
                }
                if (!iXSh_bool)
                    return NumStr_R + "整";
            }

            for (int b = 0; b < NumSr_X.Length; b++)
            {//小数部分转换
                NumStr_DQ = NumSr_X.Substring(b, 1);
                if (int.Parse(NumStr_DQ) != 0)
                    NumStr_R += Ls_ShZ[int.Parse(NumStr_DQ)] + Ls_DW_X[b];
                else if (b != 1 && iZhSh_bool)
                    NumStr_R += Ls_ShZ[int.Parse(NumStr_DQ)];
            }

            return NumStr_R;

        }
    }

    /// <summary>
    /// 获得汉字的拼音
    /// </summary>
    static public class PinyinHelper
    {
        static private Hashtable _pinyinHash;

        static PinyinHelper()
        {
            _pinyinHash = new Hashtable();
            _pinyinHash.Add(-20319, "a");
            _pinyinHash.Add(-20317, "ai"); _pinyinHash.Add(-20304, "an"); _pinyinHash.Add(-20295, "ang");
            _pinyinHash.Add(-20292, "ao"); _pinyinHash.Add(-20283, "ba"); _pinyinHash.Add(-20265, "bai");
            _pinyinHash.Add(-20257, "ban"); _pinyinHash.Add(-20242, "bang"); _pinyinHash.Add(-20230, "bao");
            _pinyinHash.Add(-20051, "bei"); _pinyinHash.Add(-20036, "ben"); _pinyinHash.Add(-20032, "beng");
            _pinyinHash.Add(-20026, "bi"); _pinyinHash.Add(-20002, "bian"); _pinyinHash.Add(-19990, "biao");
            _pinyinHash.Add(-19986, "bie"); _pinyinHash.Add(-19982, "bin"); _pinyinHash.Add(-19976, "bing");
            _pinyinHash.Add(-19805, "bo"); _pinyinHash.Add(-19784, "bu"); _pinyinHash.Add(-19775, "ca");
            _pinyinHash.Add(-19774, "cai"); _pinyinHash.Add(-19763, "can"); _pinyinHash.Add(-19756, "cang");
            _pinyinHash.Add(-19751, "cao"); _pinyinHash.Add(-19746, "ce"); _pinyinHash.Add(-19741, "ceng");
            _pinyinHash.Add(-19739, "cha"); _pinyinHash.Add(-19728, "chai"); _pinyinHash.Add(-19725, "chan");
            _pinyinHash.Add(-19715, "chang"); _pinyinHash.Add(-19540, "chao"); _pinyinHash.Add(-19531, "che");
            _pinyinHash.Add(-19525, "chen"); _pinyinHash.Add(-19515, "cheng"); _pinyinHash.Add(-19500, "chi");
            _pinyinHash.Add(-19484, "chong"); _pinyinHash.Add(-19479, "chou"); _pinyinHash.Add(-19467, "chu");
            _pinyinHash.Add(-19289, "chuai"); _pinyinHash.Add(-19288, "chuan"); _pinyinHash.Add(-19281, "chuang");
            _pinyinHash.Add(-19275, "chui"); _pinyinHash.Add(-19270, "chun"); _pinyinHash.Add(-19263, "chuo");
            _pinyinHash.Add(-19261, "ci"); _pinyinHash.Add(-19249, "cong"); _pinyinHash.Add(-19243, "cou");
            _pinyinHash.Add(-19242, "cu"); _pinyinHash.Add(-19238, "cuan"); _pinyinHash.Add(-19235, "cui");
            _pinyinHash.Add(-19227, "cun"); _pinyinHash.Add(-19224, "cuo"); _pinyinHash.Add(-19218, "da");
            _pinyinHash.Add(-19212, "dai"); _pinyinHash.Add(-19038, "dan"); _pinyinHash.Add(-19023, "dang");
            _pinyinHash.Add(-19018, "dao"); _pinyinHash.Add(-19006, "de"); _pinyinHash.Add(-19003, "deng");
            _pinyinHash.Add(-18996, "di"); _pinyinHash.Add(-18977, "dian"); _pinyinHash.Add(-18961, "diao");
            _pinyinHash.Add(-18952, "die"); _pinyinHash.Add(-18783, "ding"); _pinyinHash.Add(-18774, "diu");
            _pinyinHash.Add(-18773, "dong"); _pinyinHash.Add(-18763, "dou"); _pinyinHash.Add(-18756, "du");
            _pinyinHash.Add(-18741, "duan"); _pinyinHash.Add(-18735, "dui"); _pinyinHash.Add(-18731, "dun");
            _pinyinHash.Add(-18722, "duo"); _pinyinHash.Add(-18710, "e"); _pinyinHash.Add(-18697, "en");
            _pinyinHash.Add(-18696, "er"); _pinyinHash.Add(-18526, "fa"); _pinyinHash.Add(-18518, "fan");
            _pinyinHash.Add(-18501, "fang"); _pinyinHash.Add(-18490, "fei"); _pinyinHash.Add(-18478, "fen");
            _pinyinHash.Add(-18463, "feng"); _pinyinHash.Add(-18448, "fo"); _pinyinHash.Add(-18447, "fou");
            _pinyinHash.Add(-18446, "fu"); _pinyinHash.Add(-18239, "ga"); _pinyinHash.Add(-18237, "gai");
            _pinyinHash.Add(-18231, "gan"); _pinyinHash.Add(-18220, "gang"); _pinyinHash.Add(-18211, "gao");
            _pinyinHash.Add(-18201, "ge"); _pinyinHash.Add(-18184, "gei"); _pinyinHash.Add(-18183, "gen");
            _pinyinHash.Add(-18181, "geng"); _pinyinHash.Add(-18012, "gong"); _pinyinHash.Add(-17997, "gou");
            _pinyinHash.Add(-17988, "gu"); _pinyinHash.Add(-17970, "gua"); _pinyinHash.Add(-17964, "guai");
            _pinyinHash.Add(-17961, "guan"); _pinyinHash.Add(-17950, "guang"); _pinyinHash.Add(-17947, "gui");
            _pinyinHash.Add(-17931, "gun"); _pinyinHash.Add(-17928, "guo"); _pinyinHash.Add(-17922, "ha");
            _pinyinHash.Add(-17759, "hai"); _pinyinHash.Add(-17752, "han"); _pinyinHash.Add(-17733, "hang");
            _pinyinHash.Add(-17730, "hao"); _pinyinHash.Add(-17721, "he"); _pinyinHash.Add(-17703, "hei");
            _pinyinHash.Add(-17701, "hen"); _pinyinHash.Add(-17697, "heng"); _pinyinHash.Add(-17692, "hong");
            _pinyinHash.Add(-17683, "hou"); _pinyinHash.Add(-17676, "hu"); _pinyinHash.Add(-17496, "hua");
            _pinyinHash.Add(-17487, "huai"); _pinyinHash.Add(-17482, "huan"); _pinyinHash.Add(-17468, "huang");
            _pinyinHash.Add(-17454, "hui"); _pinyinHash.Add(-17433, "hun"); _pinyinHash.Add(-17427, "huo");
            _pinyinHash.Add(-17417, "ji"); _pinyinHash.Add(-17202, "jia"); _pinyinHash.Add(-17185, "jian");
            _pinyinHash.Add(-16983, "jiang"); _pinyinHash.Add(-16970, "jiao"); _pinyinHash.Add(-16942, "jie");
            _pinyinHash.Add(-16915, "jin"); _pinyinHash.Add(-16733, "jing"); _pinyinHash.Add(-16708, "jiong");
            _pinyinHash.Add(-16706, "jiu"); _pinyinHash.Add(-16689, "ju"); _pinyinHash.Add(-16664, "juan");
            _pinyinHash.Add(-16657, "jue"); _pinyinHash.Add(-16647, "jun"); _pinyinHash.Add(-16474, "ka");
            _pinyinHash.Add(-16470, "kai"); _pinyinHash.Add(-16465, "kan"); _pinyinHash.Add(-16459, "kang");
            _pinyinHash.Add(-16452, "kao"); _pinyinHash.Add(-16448, "ke"); _pinyinHash.Add(-16433, "ken");
            _pinyinHash.Add(-16429, "keng"); _pinyinHash.Add(-16427, "kong"); _pinyinHash.Add(-16423, "kou");
            _pinyinHash.Add(-16419, "ku"); _pinyinHash.Add(-16412, "kua"); _pinyinHash.Add(-16407, "kuai");
            _pinyinHash.Add(-16403, "kuan"); _pinyinHash.Add(-16401, "kuang"); _pinyinHash.Add(-16393, "kui");
            _pinyinHash.Add(-16220, "kun"); _pinyinHash.Add(-16216, "kuo"); _pinyinHash.Add(-16212, "la");
            _pinyinHash.Add(-16205, "lai"); _pinyinHash.Add(-16202, "lan"); _pinyinHash.Add(-16187, "lang");
            _pinyinHash.Add(-16180, "lao"); _pinyinHash.Add(-16171, "le"); _pinyinHash.Add(-16169, "lei");
            _pinyinHash.Add(-16158, "leng"); _pinyinHash.Add(-16155, "li"); _pinyinHash.Add(-15959, "lia");
            _pinyinHash.Add(-15958, "lian"); _pinyinHash.Add(-15944, "liang"); _pinyinHash.Add(-15933, "liao");
            _pinyinHash.Add(-15920, "lie"); _pinyinHash.Add(-15915, "lin"); _pinyinHash.Add(-15903, "ling");
            _pinyinHash.Add(-15889, "liu"); _pinyinHash.Add(-15878, "long"); _pinyinHash.Add(-15707, "lou");
            _pinyinHash.Add(-15701, "lu"); _pinyinHash.Add(-15681, "lv"); _pinyinHash.Add(-15667, "luan");
            _pinyinHash.Add(-15661, "lue"); _pinyinHash.Add(-15659, "lun"); _pinyinHash.Add(-15652, "luo");
            _pinyinHash.Add(-15640, "ma"); _pinyinHash.Add(-15631, "mai"); _pinyinHash.Add(-15625, "man");
            _pinyinHash.Add(-15454, "mang"); _pinyinHash.Add(-15448, "mao"); _pinyinHash.Add(-15436, "me");
            _pinyinHash.Add(-15435, "mei"); _pinyinHash.Add(-15419, "men"); _pinyinHash.Add(-15416, "meng");
            _pinyinHash.Add(-15408, "mi"); _pinyinHash.Add(-15394, "mian"); _pinyinHash.Add(-15385, "miao");
            _pinyinHash.Add(-15377, "mie"); _pinyinHash.Add(-15375, "min"); _pinyinHash.Add(-15369, "ming");
            _pinyinHash.Add(-15363, "miu"); _pinyinHash.Add(-15362, "mo"); _pinyinHash.Add(-15183, "mou");
            _pinyinHash.Add(-15180, "mu"); _pinyinHash.Add(-15165, "na"); _pinyinHash.Add(-15158, "nai");
            _pinyinHash.Add(-15153, "nan"); _pinyinHash.Add(-15150, "nang"); _pinyinHash.Add(-15149, "nao");
            _pinyinHash.Add(-15144, "ne"); _pinyinHash.Add(-15143, "nei"); _pinyinHash.Add(-15141, "nen");
            _pinyinHash.Add(-15140, "neng"); _pinyinHash.Add(-15139, "ni"); _pinyinHash.Add(-15128, "nian");
            _pinyinHash.Add(-15121, "niang"); _pinyinHash.Add(-15119, "niao"); _pinyinHash.Add(-15117, "nie");
            _pinyinHash.Add(-15110, "nin"); _pinyinHash.Add(-15109, "ning"); _pinyinHash.Add(-14941, "niu");
            _pinyinHash.Add(-14937, "nong"); _pinyinHash.Add(-14933, "nu"); _pinyinHash.Add(-14930, "nv");
            _pinyinHash.Add(-14929, "nuan"); _pinyinHash.Add(-14928, "nue"); _pinyinHash.Add(-14926, "nuo");
            _pinyinHash.Add(-14922, "o"); _pinyinHash.Add(-14921, "ou"); _pinyinHash.Add(-14914, "pa");
            _pinyinHash.Add(-14908, "pai"); _pinyinHash.Add(-14902, "pan"); _pinyinHash.Add(-14894, "pang");
            _pinyinHash.Add(-14889, "pao"); _pinyinHash.Add(-14882, "pei"); _pinyinHash.Add(-14873, "pen");
            _pinyinHash.Add(-14871, "peng"); _pinyinHash.Add(-14857, "pi"); _pinyinHash.Add(-14678, "pian");
            _pinyinHash.Add(-14674, "piao"); _pinyinHash.Add(-14670, "pie"); _pinyinHash.Add(-14668, "pin");
            _pinyinHash.Add(-14663, "ping"); _pinyinHash.Add(-14654, "po"); _pinyinHash.Add(-14645, "pu");
            _pinyinHash.Add(-14630, "qi"); _pinyinHash.Add(-14594, "qia"); _pinyinHash.Add(-14429, "qian");
            _pinyinHash.Add(-14407, "qiang"); _pinyinHash.Add(-14399, "qiao"); _pinyinHash.Add(-14384, "qie");
            _pinyinHash.Add(-14379, "qin"); _pinyinHash.Add(-14368, "qing"); _pinyinHash.Add(-14355, "qiong");
            _pinyinHash.Add(-14353, "qiu"); _pinyinHash.Add(-14345, "qu"); _pinyinHash.Add(-14170, "quan");
            _pinyinHash.Add(-14159, "que"); _pinyinHash.Add(-14151, "qun"); _pinyinHash.Add(-14149, "ran");
            _pinyinHash.Add(-14145, "rang"); _pinyinHash.Add(-14140, "rao"); _pinyinHash.Add(-14137, "re");
            _pinyinHash.Add(-14135, "ren"); _pinyinHash.Add(-14125, "reng"); _pinyinHash.Add(-14123, "ri");
            _pinyinHash.Add(-14122, "rong"); _pinyinHash.Add(-14112, "rou"); _pinyinHash.Add(-14109, "ru");
            _pinyinHash.Add(-14099, "ruan"); _pinyinHash.Add(-14097, "rui"); _pinyinHash.Add(-14094, "run");
            _pinyinHash.Add(-14092, "ruo"); _pinyinHash.Add(-14090, "sa"); _pinyinHash.Add(-14087, "sai");
            _pinyinHash.Add(-14083, "san"); _pinyinHash.Add(-13917, "sang"); _pinyinHash.Add(-13914, "sao");
            _pinyinHash.Add(-13910, "se"); _pinyinHash.Add(-13907, "sen"); _pinyinHash.Add(-13906, "seng");
            _pinyinHash.Add(-13905, "sha"); _pinyinHash.Add(-13896, "shai"); _pinyinHash.Add(-13894, "shan");
            _pinyinHash.Add(-13878, "shang"); _pinyinHash.Add(-13870, "shao"); _pinyinHash.Add(-13859, "she");
            _pinyinHash.Add(-13847, "shen"); _pinyinHash.Add(-13831, "sheng"); _pinyinHash.Add(-13658, "shi");
            _pinyinHash.Add(-13611, "shou"); _pinyinHash.Add(-13601, "shu"); _pinyinHash.Add(-13406, "shua");
            _pinyinHash.Add(-13404, "shuai"); _pinyinHash.Add(-13400, "shuan"); _pinyinHash.Add(-13398, "shuang");
            _pinyinHash.Add(-13395, "shui"); _pinyinHash.Add(-13391, "shun"); _pinyinHash.Add(-13387, "shuo");
            _pinyinHash.Add(-13383, "si"); _pinyinHash.Add(-13367, "song"); _pinyinHash.Add(-13359, "sou");
            _pinyinHash.Add(-13356, "su"); _pinyinHash.Add(-13343, "suan"); _pinyinHash.Add(-13340, "sui");
            _pinyinHash.Add(-13329, "sun"); _pinyinHash.Add(-13326, "suo"); _pinyinHash.Add(-13318, "ta");
            _pinyinHash.Add(-13147, "tai"); _pinyinHash.Add(-13138, "tan"); _pinyinHash.Add(-13120, "tang");
            _pinyinHash.Add(-13107, "tao"); _pinyinHash.Add(-13096, "te"); _pinyinHash.Add(-13095, "teng");
            _pinyinHash.Add(-13091, "ti"); _pinyinHash.Add(-13076, "tian"); _pinyinHash.Add(-13068, "tiao");
            _pinyinHash.Add(-13063, "tie"); _pinyinHash.Add(-13060, "ting"); _pinyinHash.Add(-12888, "tong");
            _pinyinHash.Add(-12875, "tou"); _pinyinHash.Add(-12871, "tu"); _pinyinHash.Add(-12860, "tuan");
            _pinyinHash.Add(-12858, "tui"); _pinyinHash.Add(-12852, "tun"); _pinyinHash.Add(-12849, "tuo");
            _pinyinHash.Add(-12838, "wa"); _pinyinHash.Add(-12831, "wai"); _pinyinHash.Add(-12829, "wan");
            _pinyinHash.Add(-12812, "wang"); _pinyinHash.Add(-12802, "wei"); _pinyinHash.Add(-12607, "wen");
            _pinyinHash.Add(-12597, "weng"); _pinyinHash.Add(-12594, "wo"); _pinyinHash.Add(-12585, "wu");
            _pinyinHash.Add(-12556, "xi"); _pinyinHash.Add(-12359, "xia"); _pinyinHash.Add(-12346, "xian");
            _pinyinHash.Add(-12320, "xiang"); _pinyinHash.Add(-12300, "xiao"); _pinyinHash.Add(-12120, "xie");
            _pinyinHash.Add(-12099, "xin"); _pinyinHash.Add(-12089, "xing"); _pinyinHash.Add(-12074, "xiong");
            _pinyinHash.Add(-12067, "xiu"); _pinyinHash.Add(-12058, "xu"); _pinyinHash.Add(-12039, "xuan");
            _pinyinHash.Add(-11867, "xue"); _pinyinHash.Add(-11861, "xun"); _pinyinHash.Add(-11847, "ya");
            _pinyinHash.Add(-11831, "yan"); _pinyinHash.Add(-11798, "yang"); _pinyinHash.Add(-11781, "yao");
            _pinyinHash.Add(-11604, "ye"); _pinyinHash.Add(-11589, "yi"); _pinyinHash.Add(-11536, "yin");
            _pinyinHash.Add(-11358, "ying"); _pinyinHash.Add(-11340, "yo"); _pinyinHash.Add(-11339, "yong");
            _pinyinHash.Add(-11324, "you"); _pinyinHash.Add(-11303, "yu"); _pinyinHash.Add(-11097, "yuan");
            _pinyinHash.Add(-11077, "yue"); _pinyinHash.Add(-11067, "yun"); _pinyinHash.Add(-11055, "za");
            _pinyinHash.Add(-11052, "zai"); _pinyinHash.Add(-11045, "zan"); _pinyinHash.Add(-11041, "zang");
            _pinyinHash.Add(-11038, "zao"); _pinyinHash.Add(-11024, "ze"); _pinyinHash.Add(-11020, "zei");
            _pinyinHash.Add(-11019, "zen"); _pinyinHash.Add(-11018, "zeng"); _pinyinHash.Add(-11014, "zha");
            _pinyinHash.Add(-10838, "zhai"); _pinyinHash.Add(-10832, "zhan"); _pinyinHash.Add(-10815, "zhang");
            _pinyinHash.Add(-10800, "zhao"); _pinyinHash.Add(-10790, "zhe"); _pinyinHash.Add(-10780, "zhen");
            _pinyinHash.Add(-10764, "zheng"); _pinyinHash.Add(-10587, "zhi"); _pinyinHash.Add(-10544, "zhong");
            _pinyinHash.Add(-10533, "zhou"); _pinyinHash.Add(-10519, "zhu"); _pinyinHash.Add(-10331, "zhua");
            _pinyinHash.Add(-10329, "zhuai"); _pinyinHash.Add(-10328, "zhuan"); _pinyinHash.Add(-10322, "zhuang");
            _pinyinHash.Add(-10315, "zhui"); _pinyinHash.Add(-10309, "zhun"); _pinyinHash.Add(-10307, "zhuo");
            _pinyinHash.Add(-10296, "zi"); _pinyinHash.Add(-10281, "zong"); _pinyinHash.Add(-10274, "zou");
            _pinyinHash.Add(-10270, "zu"); _pinyinHash.Add(-10262, "zuan"); _pinyinHash.Add(-10260, "zui");
            _pinyinHash.Add(-10256, "zun"); _pinyinHash.Add(-10254, "zuo"); _pinyinHash.Add(-10247, "zz");
        }

        /// <summary>
        /// 获得汉字的拼音，如果输入的是英文字符将原样输出，中文标点符号将被忽略
        /// </summary>
        /// <param name="chineseChars">汉字字符串</param>
        /// <returns>拼音</returns>
        static public string GetPinyin(string chineseChars)
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(chineseChars);
            int byteValue;
            StringBuilder sb = new StringBuilder(chineseChars.Length * 4);
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteValue = (int)byteArray[i];
                if (byteValue > 160)
                {
                    byteValue = byteValue * 256 + byteArray[++i] - 65536;
                    sb.Append(GetPinyin(byteValue));
                }
                else
                {
                    sb.Append((char)byteValue);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将中文转换为拼音
        /// </summary>
        /// <param name="cnStr"></param>
        /// <returns></returns>
        static public string ChangeChinesetoPinYin(string cnStr)
        {
            cnStr = HttpUtility.UrlDecode(cnStr);

            string tempResult = PinyinHelper.GetPinyin(cnStr);

            Regex rex = new Regex("[a-z0-9A-Z_]+");
            MatchCollection mc = rex.Matches(tempResult);

            string retStr = "";

            foreach (Match m in mc)
            {
                retStr += m.ToString();
            }

            return retStr;
        }

        /// <summary>
        /// 获得汉字拼音的简写，即每一个汉字的拼音的首字母组成的串，如果输入的是英文字符将原样输出，中文标点符号将被忽略
        /// </summary>
        /// <param name="chineseChars">汉字字符串</param>
        /// <returns>拼音简写</returns>
        static public string GetShortPinyin(string chineseChars)
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(chineseChars);
            int byteValue;
            StringBuilder sb = new StringBuilder(chineseChars.Length * 4);
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteValue = (int)byteArray[i];
                if (byteValue > 160)
                {
                    byteValue = byteValue * 256 + byteArray[++i] - 65536;
                    string charPinyin = GetPinyin(byteValue);
                    if (!string.IsNullOrEmpty(charPinyin))
                    {
                        charPinyin = new string(charPinyin[0], 1);
                    }
                    sb.Append(charPinyin);
                }
                else
                {
                    sb.Append((char)byteValue);
                }
            }

            return sb.ToString();
        }

        static private string GetPinyin(int charValue)
        {
            if (charValue < -20319 || charValue > -10247)
                return "";

            while (!_pinyinHash.ContainsKey(charValue))
                charValue--;

            return (string)_pinyinHash[charValue];
        }

    }



    public abstract class CNToPinyin
    {
        private const string strChineseFirstPY =
"YDYQSXMWZSSXJBYMGCCZQPSSQBYCDSCDQLDYLYBSSJGYZZJJFKCCLZDHWDWZJLJPFYYNWJJTMYHZWZHFLZPPQHGSCYYYNJQYXXGJ"
+ "HHSDSJNKKTMOMLCRXYPSNQSECCQZGGLLYJLMYZZSECYKYYHQWJSSGGYXYZYJWWKDJHYCHMYXJTLXJYQBYXZLDWRDJRWYSRLDZJPC"
+ "BZJJBRCFTLECZSTZFXXZHTRQHYBDLYCZSSYMMRFMYQZPWWJJYFCRWFDFZQPYDDWYXKYJAWJFFXYPSFTZYHHYZYSWCJYXSCLCXXWZ"
+ "ZXNBGNNXBXLZSZSBSGPYSYZDHMDZBQBZCWDZZYYTZHBTSYYBZGNTNXQYWQSKBPHHLXGYBFMJEBJHHGQTJCYSXSTKZHLYCKGLYSMZ"
+ "XYALMELDCCXGZYRJXSDLTYZCQKCNNJWHJTZZCQLJSTSTBNXBTYXCEQXGKWJYFLZQLYHYXSPSFXLMPBYSXXXYDJCZYLLLSJXFHJXP"
+ "JBTFFYABYXBHZZBJYZLWLCZGGBTSSMDTJZXPTHYQTGLJSCQFZKJZJQNLZWLSLHDZBWJNCJZYZSQQYCQYRZCJJWYBRTWPYFTWEXCS"
+ "KDZCTBZHYZZYYJXZCFFZZMJYXXSDZZOTTBZLQWFCKSZSXFYRLNYJMBDTHJXSQQCCSBXYYTSYFBXDZTGBCNSLCYZZPSAZYZZSCJCS"
+ "HZQYDXLBPJLLMQXTYDZXSQJTZPXLCGLQTZWJBHCTSYJSFXYEJJTLBGXSXJMYJQQPFZASYJNTYDJXKJCDJSZCBARTDCLYJQMWNQNC"
+ "LLLKBYBZZSYHQQLTWLCCXTXLLZNTYLNEWYZYXCZXXGRKRMTCNDNJTSYYSSDQDGHSDBJGHRWRQLYBGLXHLGTGXBQJDZPYJSJYJCTM"
+ "RNYMGRZJCZGJMZMGXMPRYXKJNYMSGMZJYMKMFXMLDTGFBHCJHKYLPFMDXLQJJSMTQGZSJLQDLDGJYCALCMZCSDJLLNXDJFFFFJCZ"
+ "FMZFFPFKHKGDPSXKTACJDHHZDDCRRCFQYJKQCCWJDXHWJLYLLZGCFCQDSMLZPBJJPLSBCJGGDCKKDEZSQCCKJGCGKDJTJDLZYCXK"
+ "LQSCGJCLTFPCQCZGWPJDQYZJJBYJHSJDZWGFSJGZKQCCZLLPSPKJGQJHZZLJPLGJGJJTHJJYJZCZMLZLYQBGJWMLJKXZDZNJQSYZ"
+ "MLJLLJKYWXMKJLHSKJGBMCLYYMKXJQLBMLLKMDXXKWYXYSLMLPSJQQJQXYXFJTJDXMXXLLCXQBSYJBGWYMBGGBCYXPJYGPEPFGDJ"
+ "GBHBNSQJYZJKJKHXQFGQZKFHYGKHDKLLSDJQXPQYKYBNQSXQNSZSWHBSXWHXWBZZXDMNSJBSBKBBZKLYLXGWXDRWYQZMYWSJQLCJ"
+ "XXJXKJEQXSCYETLZHLYYYSDZPAQYZCMTLSHTZCFYZYXYLJSDCJQAGYSLCQLYYYSHMRQQKLDXZSCSSSYDYCJYSFSJBFRSSZQSBXXP"
+ "XJYSDRCKGJLGDKZJZBDKTCSYQPYHSTCLDJDHMXMCGXYZHJDDTMHLTXZXYLYMOHYJCLTYFBQQXPFBDFHHTKSQHZYYWCNXXCRWHOWG"
+ "YJLEGWDQCWGFJYCSNTMYTOLBYGWQWESJPWNMLRYDZSZTXYQPZGCWXHNGPYXSHMYQJXZTDPPBFYHZHTJYFDZWKGKZBLDNTSXHQEEG"
+ "ZZYLZMMZYJZGXZXKHKSTXNXXWYLYAPSTHXDWHZYMPXAGKYDXBHNHXKDPJNMYHYLPMGOCSLNZHKXXLPZZLBMLSFBHHGYGYYGGBHSC"
+ "YAQTYWLXTZQCEZYDQDQMMHTKLLSZHLSJZWFYHQSWSCWLQAZYNYTLSXTHAZNKZZSZZLAXXZWWCTGQQTDDYZTCCHYQZFLXPSLZYGPZ"
+ "SZNGLNDQTBDLXGTCTAJDKYWNSYZLJHHZZCWNYYZYWMHYCHHYXHJKZWSXHZYXLYSKQYSPSLYZWMYPPKBYGLKZHTYXAXQSYSHXASMC"
+ "HKDSCRSWJPWXSGZJLWWSCHSJHSQNHCSEGNDAQTBAALZZMSSTDQJCJKTSCJAXPLGGXHHGXXZCXPDMMHLDGTYBYSJMXHMRCPXXJZCK"
+ "ZXSHMLQXXTTHXWZFKHCCZDYTCJYXQHLXDHYPJQXYLSYYDZOZJNYXQEZYSQYAYXWYPDGXDDXSPPYZNDLTWRHXYDXZZJHTCXMCZLHP"
+ "YYYYMHZLLHNXMYLLLMDCPPXHMXDKYCYRDLTXJCHHZZXZLCCLYLNZSHZJZZLNNRLWHYQSNJHXYNTTTKYJPYCHHYEGKCTTWLGQRLGG"
+ "TGTYGYHPYHYLQYQGCWYQKPYYYTTTTLHYHLLTYTTSPLKYZXGZWGPYDSSZZDQXSKCQNMJJZZBXYQMJRTFFBTKHZKBXLJJKDXJTLBWF"
+ "ZPPTKQTZTGPDGNTPJYFALQMKGXBDCLZFHZCLLLLADPMXDJHLCCLGYHDZFGYDDGCYYFGYDXKSSEBDHYKDKDKHNAXXYBPBYYHXZQGA"
+ "FFQYJXDMLJCSQZLLPCHBSXGJYNDYBYQSPZWJLZKSDDTACTBXZDYZYPJZQSJNKKTKNJDJGYYPGTLFYQKASDNTCYHBLWDZHBBYDWJR"
+ "YGKZYHEYYFJMSDTYFZJJHGCXPLXHLDWXXJKYTCYKSSSMTWCTTQZLPBSZDZWZXGZAGYKTYWXLHLSPBCLLOQMMZSSLCMBJCSZZKYDC"
+ "ZJGQQDSMCYTZQQLWZQZXSSFPTTFQMDDZDSHDTDWFHTDYZJYQJQKYPBDJYYXTLJHDRQXXXHAYDHRJLKLYTWHLLRLLRCXYLBWSRSZZ"
+ "SYMKZZHHKYHXKSMDSYDYCJPBZBSQLFCXXXNXKXWYWSDZYQOGGQMMYHCDZTTFJYYBGSTTTYBYKJDHKYXBELHTYPJQNFXFDYKZHQKZ"
+ "BYJTZBXHFDXKDASWTAWAJLDYJSFHBLDNNTNQJTJNCHXFJSRFWHZFMDRYJYJWZPDJKZYJYMPCYZNYNXFBYTFYFWYGDBNZZZDNYTXZ"
+ "EMMQBSQEHXFZMBMFLZZSRXYMJGSXWZJSPRYDJSJGXHJJGLJJYNZZJXHGXKYMLPYYYCXYTWQZSWHWLYRJLPXSLSXMFSWWKLCTNXNY"
+ "NPSJSZHDZEPTXMYYWXYYSYWLXJQZQXZDCLEEELMCPJPCLWBXSQHFWWTFFJTNQJHJQDXHWLBYZNFJLALKYYJLDXHHYCSTYYWNRJYX"
+ "YWTRMDRQHWQCMFJDYZMHMYYXJWMYZQZXTLMRSPWWCHAQBXYGZYPXYYRRCLMPYMGKSJSZYSRMYJSNXTPLNBAPPYPYLXYYZKYNLDZY"
+ "JZCZNNLMZHHARQMPGWQTZMXXMLLHGDZXYHXKYXYCJMFFYYHJFSBSSQLXXNDYCANNMTCJCYPRRNYTYQNYYMBMSXNDLYLYSLJRLXYS"
+ "XQMLLYZLZJJJKYZZCSFBZXXMSTBJGNXYZHLXNMCWSCYZYFZLXBRNNNYLBNRTGZQYSATSWRYHYJZMZDHZGZDWYBSSCSKXSYHYTXXG"
+ "CQGXZZSHYXJSCRHMKKBXCZJYJYMKQHZJFNBHMQHYSNJNZYBKNQMCLGQHWLZNZSWXKHLJHYYBQLBFCDSXDLDSPFZPSKJYZWZXZDDX"
+ "JSMMEGJSCSSMGCLXXKYYYLNYPWWWGYDKZJGGGZGGSYCKNJWNJPCXBJJTQTJWDSSPJXZXNZXUMELPXFSXTLLXCLJXJJLJZXCTPSWX"
+ "LYDHLYQRWHSYCSQYYBYAYWJJJQFWQCQQCJQGXALDBZZYJGKGXPLTZYFXJLTPADKYQHPMATLCPDCKBMTXYBHKLENXDLEEGQDYMSAW"
+ "HZMLJTWYGXLYQZLJEEYYBQQFFNLYXRDSCTGJGXYYNKLLYQKCCTLHJLQMKKZGCYYGLLLJDZGYDHZWXPYSJBZKDZGYZZHYWYFQYTYZ"
+ "SZYEZZLYMHJJHTSMQWYZLKYYWZCSRKQYTLTDXWCTYJKLWSQZWBDCQYNCJSRSZJLKCDCDTLZZZACQQZZDDXYPLXZBQJYLZLLLQDDZ"
+ "QJYJYJZYXNYYYNYJXKXDAZWYRDLJYYYRJLXLLDYXJCYWYWNQCCLDDNYYYNYCKCZHXXCCLGZQJGKWPPCQQJYSBZZXYJSQPXJPZBSB"
+ "DSFNSFPZXHDWZTDWPPTFLZZBZDMYYPQJRSDZSQZSQXBDGCPZSWDWCSQZGMDHZXMWWFYBPDGPHTMJTHZSMMBGZMBZJCFZWFZBBZMQ"
+ "CFMBDMCJXLGPNJBBXGYHYYJGPTZGZMQBQTCGYXJXLWZKYDPDYMGCFTPFXYZTZXDZXTGKMTYBBCLBJASKYTSSQYYMSZXFJEWLXLLS"
+ "ZBQJJJAKLYLXLYCCTSXMCWFKKKBSXLLLLJYXTYLTJYYTDPJHNHNNKBYQNFQYYZBYYESSESSGDYHFHWTCJBSDZZTFDMXHCNJZYMQW"
+ "SRYJDZJQPDQBBSTJGGFBKJBXTGQHNGWJXJGDLLTHZHHYYYYYYSXWTYYYCCBDBPYPZYCCZYJPZYWCBDLFWZCWJDXXHYHLHWZZXJTC"
+ "ZLCDPXUJCZZZLYXJJTXPHFXWPYWXZPTDZZBDZCYHJHMLXBQXSBYLRDTGJRRCTTTHYTCZWMXFYTWWZCWJWXJYWCSKYBZSCCTZQNHX"
+ "NWXXKHKFHTSWOCCJYBCMPZZYKBNNZPBZHHZDLSYDDYTYFJPXYNGFXBYQXCBHXCPSXTYZDMKYSNXSXLHKMZXLYHDHKWHXXSSKQYHH"
+ "CJYXGLHZXCSNHEKDTGZXQYPKDHEXTYKCNYMYYYPKQYYYKXZLTHJQTBYQHXBMYHSQCKWWYLLHCYYLNNEQXQWMCFBDCCMLJGGXDQKT"
+ "LXKGNQCDGZJWYJJLYHHQTTTNWCHMXCXWHWSZJYDJCCDBQCDGDNYXZTHCQRXCBHZTQCBXWGQWYYBXHMBYMYQTYEXMQKYAQYRGYZSL"
+ "FYKKQHYSSQYSHJGJCNXKZYCXSBXYXHYYLSTYCXQTHYSMGSCPMMGCCCCCMTZTASMGQZJHKLOSQYLSWTMXSYQKDZLJQQYPLSYCZTCQ"
+ "QPBBQJZCLPKHQZYYXXDTDDTSJCXFFLLCHQXMJLWCJCXTSPYCXNDTJSHJWXDQQJSKXYAMYLSJHMLALYKXCYYDMNMDQMXMCZNNCYBZ"
+ "KKYFLMCHCMLHXRCJJHSYLNMTJZGZGYWJXSRXCWJGJQHQZDQJDCJJZKJKGDZQGJJYJYLXZXXCDQHHHEYTMHLFSBDJSYYSHFYSTCZQ"
+ "LPBDRFRZTZYKYWHSZYQKWDQZRKMSYNBCRXQBJYFAZPZZEDZCJYWBCJWHYJBQSZYWRYSZPTDKZPFPBNZTKLQYHBBZPNPPTYZZYBQN"
+ "YDCPJMMCYCQMCYFZZDCMNLFPBPLNGQJTBTTNJZPZBBZNJKLJQYLNBZQHKSJZNGGQSZZKYXSHPZSNBCGZKDDZQANZHJKDRTLZLSWJ"
+ "LJZLYWTJNDJZJHXYAYNCBGTZCSSQMNJPJYTYSWXZFKWJQTKHTZPLBHSNJZSYZBWZZZZLSYLSBJHDWWQPSLMMFBJDWAQYZTCJTBNN"
+ "WZXQXCDSLQGDSDPDZHJTQQPSWLYYJZLGYXYZLCTCBJTKTYCZJTQKBSJLGMGZDMCSGPYNJZYQYYKNXRPWSZXMTNCSZZYXYBYHYZAX"
+ "YWQCJTLLCKJJTJHGDXDXYQYZZBYWDLWQCGLZGJGQRQZCZSSBCRPCSKYDZNXJSQGXSSJMYDNSTZTPBDLTKZWXQWQTZEXNQCZGWEZK"
+ "SSBYBRTSSSLCCGBPSZQSZLCCGLLLZXHZQTHCZMQGYZQZNMCOCSZJMMZSQPJYGQLJYJPPLDXRGZYXCCSXHSHGTZNLZWZKJCXTCFCJ"
+ "XLBMQBCZZWPQDNHXLJCTHYZLGYLNLSZZPCXDSCQQHJQKSXZPBAJYEMSMJTZDXLCJYRYYNWJBNGZZTMJXLTBSLYRZPYLSSCNXPHLL"
+ "HYLLQQZQLXYMRSYCXZLMMCZLTZSDWTJJLLNZGGQXPFSKYGYGHBFZPDKMWGHCXMSGDXJMCJZDYCABXJDLNBCDQYGSKYDQTXDJJYXM"
+ "SZQAZDZFSLQXYJSJZYLBTXXWXQQZBJZUFBBLYLWDSLJHXJYZJWTDJCZFQZQZZDZSXZZQLZCDZFJHYSPYMPQZMLPPLFFXJJNZZYLS"
+ "JEYQZFPFZKSYWJJJHRDJZZXTXXGLGHYDXCSKYSWMMZCWYBAZBJKSHFHJCXMHFQHYXXYZFTSJYZFXYXPZLCHMZMBXHZZSXYFYMNCW"
+ "DABAZLXKTCSHHXKXJJZJSTHYGXSXYYHHHJWXKZXSSBZZWHHHCWTZZZPJXSNXQQJGZYZYWLLCWXZFXXYXYHXMKYYSWSQMNLNAYCYS"
+ "PMJKHWCQHYLAJJMZXHMMCNZHBHXCLXTJPLTXYJHDYYLTTXFSZHYXXSJBJYAYRSMXYPLCKDUYHLXRLNLLSTYZYYQYGYHHSCCSMZCT"
+ "ZQXKYQFPYYRPFFLKQUNTSZLLZMWWTCQQYZWTLLMLMPWMBZSSTZRBPDDTLQJJBXZCSRZQQYGWCSXFWZLXCCRSZDZMCYGGDZQSGTJS"
+ "WLJMYMMZYHFBJDGYXCCPSHXNZCSBSJYJGJMPPWAFFYFNXHYZXZYLREMZGZCYZSSZDLLJCSQFNXZKPTXZGXJJGFMYYYSNBTYLBNLH"
+ "PFZDCYFBMGQRRSSSZXYSGTZRNYDZZCDGPJAFJFZKNZBLCZSZPSGCYCJSZLMLRSZBZZLDLSLLYSXSQZQLYXZLSKKBRXBRBZCYCXZZ"
+ "ZEEYFGKLZLYYHGZSGZLFJHGTGWKRAAJYZKZQTSSHJJXDCYZUYJLZYRZDQQHGJZXSSZBYKJPBFRTJXLLFQWJHYLQTYMBLPZDXTZYG"
+ "BDHZZRBGXHWNJTJXLKSCFSMWLSDQYSJTXKZSCFWJLBXFTZLLJZLLQBLSQMQQCGCZFPBPHZCZJLPYYGGDTGWDCFCZQYYYQYSSCLXZ"
+ "SKLZZZGFFCQNWGLHQYZJJCZLQZZYJPJZZBPDCCMHJGXDQDGDLZQMFGPSYTSDYFWWDJZJYSXYYCZCYHZWPBYKXRYLYBHKJKSFXTZJ"
+ "MMCKHLLTNYYMSYXYZPYJQYCSYCWMTJJKQYRHLLQXPSGTLYYCLJSCPXJYZFNMLRGJJTYZBXYZMSJYJHHFZQMSYXRSZCWTLRTQZSST"
+ "KXGQKGSPTGCZNJSJCQCXHMXGGZTQYDJKZDLBZSXJLHYQGGGTHQSZPYHJHHGYYGKGGCWJZZYLCZLXQSFTGZSLLLMLJSKCTBLLZZSZ"
+ "MMNYTPZSXQHJCJYQXYZXZQZCPSHKZZYSXCDFGMWQRLLQXRFZTLYSTCTMJCXJJXHJNXTNRZTZFQYHQGLLGCXSZSJDJLJCYDSJTLNY"
+ "XHSZXCGJZYQPYLFHDJSBPCCZHJJJQZJQDYBSSLLCMYTTMQTBHJQNNYGKYRQYQMZGCJKPDCGMYZHQLLSLLCLMHOLZGDYYFZSLJCQZ"
+ "LYLZQJESHNYLLJXGJXLYSYYYXNBZLJSSZCQQCJYLLZLTJYLLZLLBNYLGQCHXYYXOXCXQKYJXXXYKLXSXXYQXCYKQXQCSGYXXYQXY"
+ "GYTQOHXHXPYXXXULCYEYCHZZCBWQBBWJQZSCSZSSLZYLKDESJZWMYMCYTSDSXXSCJPQQSQYLYYZYCMDJDZYWCBTJSYDJKCYDDJLB"
+ "DJJSODZYSYXQQYXDHHGQQYQHDYXWGMMMAJDYBBBPPBCMUUPLJZSMTXERXJMHQNUTPJDCBSSMSSSTKJTSSMMTRCPLZSZMLQDSDMJM"
+ "QPNQDXCFYNBFSDQXYXHYAYKQYDDLQYYYSSZBYDSLNTFQTZQPZMCHDHCZCWFDXTMYQSPHQYYXSRGJCWTJTZZQMGWJJTJHTQJBBHWZ"
+ "PXXHYQFXXQYWYYHYSCDYDHHQMNMTMWCPBSZPPZZGLMZFOLLCFWHMMSJZTTDHZZYFFYTZZGZYSKYJXQYJZQBHMBZZLYGHGFMSHPZF"
+ "ZSNCLPBQSNJXZSLXXFPMTYJYGBXLLDLXPZJYZJYHHZCYWHJYLSJEXFSZZYWXKZJLUYDTMLYMQJPWXYHXSKTQJEZRPXXZHHMHWQPW"
+ "QLYJJQJJZSZCPHJLCHHNXJLQWZJHBMZYXBDHHYPZLHLHLGFWLCHYYTLHJXCJMSCPXSTKPNHQXSRTYXXTESYJCTLSSLSTDLLLWWYH"
+ "DHRJZSFGXTSYCZYNYHTDHWJSLHTZDQDJZXXQHGYLTZPHCSQFCLNJTCLZPFSTPDYNYLGMJLLYCQHYSSHCHYLHQYQTMZYPBYWRFQYK"
+ "QSYSLZDQJMPXYYSSRHZJNYWTQDFZBWWTWWRXCWHGYHXMKMYYYQMSMZHNGCEPMLQQMTCWCTMMPXJPJJHFXYYZSXZHTYBMSTSYJTTQ"
+ "QQYYLHYNPYQZLCYZHZWSMYLKFJXLWGXYPJYTYSYXYMZCKTTWLKSMZSYLMPWLZWXWQZSSAQSYXYRHSSNTSRAPXCPWCMGDXHXZDZYF"
+ "JHGZTTSBJHGYZSZYSMYCLLLXBTYXHBBZJKSSDMALXHYCFYGMQYPJYCQXJLLLJGSLZGQLYCJCCZOTYXMTMTTLLWTGPXYMZMKLPSZZ"
+ "ZXHKQYSXCTYJZYHXSHYXZKXLZWPSQPYHJWPJPWXQQYLXSDHMRSLZZYZWTTCYXYSZZSHBSCCSTPLWSSCJCHNLCGCHSSPHYLHFHHXJ"
+ "SXYLLNYLSZDHZXYLSXLWZYKCLDYAXZCMDDYSPJTQJZLNWQPSSSWCTSTSZLBLNXSMNYYMJQBQHRZWTYYDCHQLXKPZWBGQYBKFCMZW"
+ "PZLLYYLSZYDWHXPSBCMLJBSCGBHXLQHYRLJXYSWXWXZSLDFHLSLYNJLZYFLYJYCDRJLFSYZFSLLCQYQFGJYHYXZLYLMSTDJCYHBZ"
+ "LLNWLXXYGYYHSMGDHXXHHLZZJZXCZZZCYQZFNGWPYLCPKPYYPMCLQKDGXZGGWQBDXZZKZFBXXLZXJTPJPTTBYTSZZDWSLCHZHSLT"
+ "YXHQLHYXXXYYZYSWTXZKHLXZXZPYHGCHKCFSYHUTJRLXFJXPTZTWHPLYXFCRHXSHXKYXXYHZQDXQWULHYHMJTBFLKHTXCWHJFWJC"
+ "FPQRYQXCYYYQYGRPYWSGSUNGWCHKZDXYFLXXHJJBYZWTSXXNCYJJYMSWZJQRMHXZWFQSYLZJZGBHYNSLBGTTCSYBYXXWXYHXYYXN"
+ "SQYXMQYWRGYQLXBBZLJSYLPSYTJZYHYZAWLRORJMKSCZJXXXYXCHDYXRYXXJDTSQFXLYLTSFFYXLMTYJMJUYYYXLTZCSXQZQHZXL"
+ "YYXZHDNBRXXXJCTYHLBRLMBRLLAXKYLLLJLYXXLYCRYLCJTGJCMTLZLLCYZZPZPCYAWHJJFYBDYYZSMPCKZDQYQPBPCJPDCYZMDP"
+ "BCYYDYCNNPLMTMLRMFMMGWYZBSJGYGSMZQQQZTXMKQWGXLLPJGZBQCDJJJFPKJKCXBLJMSWMDTQJXLDLPPBXCWRCQFBFQJCZAHZG"
+ "MYKPHYYHZYKNDKZMBPJYXPXYHLFPNYYGXJDBKXNXHJMZJXSTRSTLDXSKZYSYBZXJLXYSLBZYSLHXJPFXPQNBYLLJQKYGZMCYZZYM"
+ "CCSLCLHZFWFWYXZMWSXTYNXJHPYYMCYSPMHYSMYDYSHQYZCHMJJMZCAAGCFJBBHPLYZYLXXSDJGXDHKXXTXXNBHRMLYJSLTXMRHN"
+ "LXQJXYZLLYSWQGDLBJHDCGJYQYCMHWFMJYBMBYJYJWYMDPWHXQLDYGPDFXXBCGJSPCKRSSYZJMSLBZZJFLJJJLGXZGYXYXLSZQYX"
+ "BEXYXHGCXBPLDYHWETTWWCJMBTXCHXYQXLLXFLYXLLJLSSFWDPZSMYJCLMWYTCZPCHQEKCQBWLCQYDPLQPPQZQFJQDJHYMMCXTXD"
+ "RMJWRHXCJZYLQXDYYNHYYHRSLSRSYWWZJYMTLTLLGTQCJZYABTCKZCJYCCQLJZQXALMZYHYWLWDXZXQDLLQSHGPJFJLJHJABCQZD"
+ "JGTKHSSTCYJLPSWZLXZXRWGLDLZRLZXTGSLLLLZLYXXWGDZYGBDPHZPBRLWSXQBPFDWOFMWHLYPCBJCCLDMBZPBZZLCYQXLDOMZB"
+ "LZWPDWYYGDSTTHCSQSCCRSSSYSLFYBFNTYJSZDFNDPDHDZZMBBLSLCMYFFGTJJQWFTMTPJWFNLBZCMMJTGBDZLQLPYFHYYMJYLSD"
+ "CHDZJWJCCTLJCLDTLJJCPDDSQDSSZYBNDBJLGGJZXSXNLYCYBJXQYCBYLZCFZPPGKCXZDZFZTJJFJSJXZBNZYJQTTYJYHTYCZHYM"
+ "DJXTTMPXSPLZCDWSLSHXYPZGTFMLCJTYCBPMGDKWYCYZCDSZZYHFLYCTYGWHKJYYLSJCXGYWJCBLLCSNDDBTZBSCLYZCZZSSQDLL"
+ "MQYYHFSLQLLXFTYHABXGWNYWYYPLLSDLDLLBJCYXJZMLHLJDXYYQYTDLLLBUGBFDFBBQJZZMDPJHGCLGMJJPGAEHHBWCQXAXHHHZ"
+ "CHXYPHJAXHLPHJPGPZJQCQZGJJZZUZDMQYYBZZPHYHYBWHAZYJHYKFGDPFQSDLZMLJXKXGALXZDAGLMDGXMWZQYXXDXXPFDMMSSY"
+ "MPFMDMMKXKSYZYSHDZKXSYSMMZZZMSYDNZZCZXFPLSTMZDNMXCKJMZTYYMZMZZMSXHHDCZJEMXXKLJSTLWLSQLYJZLLZJSSDPPMH"
+ "NLZJCZYHMXXHGZCJMDHXTKGRMXFWMCGMWKDTKSXQMMMFZZYDKMSCLCMPCGMHSPXQPZDSSLCXKYXTWLWJYAHZJGZQMCSNXYYMMPML"
+ "KJXMHLMLQMXCTKZMJQYSZJSYSZHSYJZJCDAJZYBSDQJZGWZQQXFKDMSDJLFWEHKZQKJPEYPZYSZCDWYJFFMZZYLTTDZZEFMZLBNP"
+ "PLPLPEPSZALLTYLKCKQZKGENQLWAGYXYDPXLHSXQQWQCQXQCLHYXXMLYCCWLYMQYSKGCHLCJNSZKPYZKCQZQLJPDMDZHLASXLBYD"
+ "WQLWDNBQCRYDDZTJYBKBWSZDXDTNPJDTCTQDFXQQMGNXECLTTBKPWSLCTYQLPWYZZKLPYGZCQQPLLKCCYLPQMZCZQCLJSLQZDJXL"
+ "DDHPZQDLJJXZQDXYZQKZLJCYQDYJPPYPQYKJYRMPCBYMCXKLLZLLFQPYLLLMBSGLCYSSLRSYSQTMXYXZQZFDZUYSYZTFFMZZSMZQ"
+ "HZSSCCMLYXWTPZGXZJGZGSJSGKDDHTQGGZLLBJDZLCBCHYXYZHZFYWXYZYMSDBZZYJGTSMTFXQYXQSTDGSLNXDLRYZZLRYYLXQHT"
+ "XSRTZNGZXBNQQZFMYKMZJBZYMKBPNLYZPBLMCNQYZZZSJZHJCTZKHYZZJRDYZHNPXGLFZTLKGJTCTSSYLLGZRZBBQZZKLPKLCZYS"
+ "SUYXBJFPNJZZXCDWXZYJXZZDJJKGGRSRJKMSMZJLSJYWQSKYHQJSXPJZZZLSNSHRNYPZTWCHKLPSRZLZXYJQXQKYSJYCZTLQZYBB"
+ "YBWZPQDWWYZCYTJCJXCKCWDKKZXSGKDZXWWYYJQYYTCYTDLLXWKCZKKLCCLZCQQDZLQLCSFQCHQHSFSMQZZLNBJJZBSJHTSZDYSJ"
+ "QJPDLZCDCWJKJZZLPYCGMZWDJJBSJQZSYZYHHXJPBJYDSSXDZNCGLQMBTSFSBPDZDLZNFGFJGFSMPXJQLMBLGQCYYXBQKDJJQYRF"
+ "KZTJDHCZKLBSDZCFJTPLLJGXHYXZCSSZZXSTJYGKGCKGYOQXJPLZPBPGTGYJZGHZQZZLBJLSQFZGKQQJZGYCZBZQTLDXRJXBSXXP"
+ "ZXHYZYCLWDXJJHXMFDZPFZHQHQMQGKSLYHTYCGFRZGNQXCLPDLBZCSCZQLLJBLHBZCYPZZPPDYMZZSGYHCKCPZJGSLJLNSCDSLDL"
+ "XBMSTLDDFJMKDJDHZLZXLSZQPQPGJLLYBDSZGQLBZLSLKYYHZTTNTJYQTZZPSZQZTLLJTYYLLQLLQYZQLBDZLSLYYZYMDFSZSNHL"
+ "XZNCZQZPBWSKRFBSYZMTHBLGJPMCZZLSTLXSHTCSYZLZBLFEQHLXFLCJLYLJQCBZLZJHHSSTBRMHXZHJZCLXFNBGXGTQJCZTMSFZ"
+ "KJMSSNXLJKBHSJXNTNLZDNTLMSJXGZJYJCZXYJYJWRWWQNZTNFJSZPZSHZJFYRDJSFSZJZBJFZQZZHZLXFYSBZQLZSGYFTZDCSZX"
+ "ZJBQMSZKJRHYJZCKMJKHCHGTXKXQGLXPXFXTRTYLXJXHDTSJXHJZJXZWZLCQSBTXWXGXTXXHXFTSDKFJHZYJFJXRZSDLLLTQSQQZ"
+ "QWZXSYQTWGWBZCGZLLYZBCLMQQTZHZXZXLJFRMYZFLXYSQXXJKXRMQDZDMMYYBSQBHGZMWFWXGMXLZPYYTGZYCCDXYZXYWGSYJYZ"
+ "NBHPZJSQSYXSXRTFYZGRHZTXSZZTHCBFCLSYXZLZQMZLMPLMXZJXSFLBYZMYQHXJSXRXSQZZZSSLYFRCZJRCRXHHZXQYDYHXSJJH"
+ "ZCXZBTYNSYSXJBQLPXZQPYMLXZKYXLXCJLCYSXXZZLXDLLLJJYHZXGYJWKJRWYHCPSGNRZLFZWFZZNSXGXFLZSXZZZBFCSYJDBRJ"
+ "KRDHHGXJLJJTGXJXXSTJTJXLYXQFCSGSWMSBCTLQZZWLZZKXJMLTMJYHSDDBXGZHDLBMYJFRZFSGCLYJBPMLYSMSXLSZJQQHJZFX"
+ "GFQFQBPXZGYYQXGZTCQWYLTLGWSGWHRLFSFGZJMGMGBGTJFSYZZGZYZAFLSSPMLPFLCWBJZCLJJMZLPJJLYMQDMYYYFBGYGYZMLY"
+ "ZDXQYXRQQQHSYYYQXYLJTYXFSFSLLGNQCYHYCWFHCCCFXPYLYPLLZYXXXXXKQHHXSHJZCFZSCZJXCPZWHHHHHAPYLQALPQAFYHXD"
+ "YLUKMZQGGGDDESRNNZLTZGCHYPPYSQJJHCLLJTOLNJPZLJLHYMHEYDYDSQYCDDHGZUNDZCLZYZLLZNTNYZGSLHSLPJJBDGWXPCDU"
+ "TJCKLKCLWKLLCASSTKZZDNQNTTLYYZSSYSSZZRYLJQKCQDHHCRXRZYDGRGCWCGZQFFFPPJFZYNAKRGYWYQPQXXFKJTSZZXSWZDDF"
+ "BBXTBGTZKZNPZZPZXZPJSZBMQHKCYXYLDKLJNYPKYGHGDZJXXEAHPNZKZTZCMXCXMMJXNKSZQNMNLWBWWXJKYHCPSTMCSQTZJYXT"
+ "PCTPDTNNPGLLLZSJLSPBLPLQHDTNJNLYYRSZFFJFQWDPHZDWMRZCCLODAXNSSNYZRESTYJWJYJDBCFXNMWTTBYLWSTSZGYBLJPXG"
+ "LBOCLHPCBJLTMXZLJYLZXCLTPNCLCKXTPZJSWCYXSFYSZDKNTLBYJCYJLLSTGQCBXRYZXBXKLYLHZLQZLNZCXWJZLJZJNCJHXMNZ"
+ "ZGJZZXTZJXYCYYCXXJYYXJJXSSSJSTSSTTPPGQTCSXWZDCSYFPTFBFHFBBLZJCLZZDBXGCXLQPXKFZFLSYLTUWBMQJHSZBMDDBCY"
+ "SCCLDXYCDDQLYJJWMQLLCSGLJJSYFPYYCCYLTJANTJJPWYCMMGQYYSXDXQMZHSZXPFTWWZQSWQRFKJLZJQQYFBRXJHHFWJJZYQAZ"
+ "MYFRHCYYBYQWLPEXCCZSTYRLTTDMQLYKMBBGMYYJPRKZNPBSXYXBHYZDJDNGHPMFSGMWFZMFQMMBCMZZCJJLCNUXYQLMLRYGQZCY"
+ "XZLWJGCJCGGMCJNFYZZJHYCPRRCMTZQZXHFQGTJXCCJEAQCRJYHPLQLSZDJRBCQHQDYRHYLYXJSYMHZYDWLDFRYHBPYDTSSCNWBX"
+ "GLPZMLZZTQSSCPJMXXYCSJYTYCGHYCJWYRXXLFEMWJNMKLLSWTXHYYYNCMMCWJDQDJZGLLJWJRKHPZGGFLCCSCZMCBLTBHBQJXQD"
+ "SPDJZZGKGLFQYWBZYZJLTSTDHQHCTCBCHFLQMPWDSHYYTQWCNZZJTLBYMBPDYYYXSQKXWYYFLXXNCWCXYPMAELYKKJMZZZBRXYYQ"
+ "JFLJPFHHHYTZZXSGQQMHSPGDZQWBWPJHZJDYSCQWZKTXXSQLZYYMYSDZGRXCKKUJLWPYSYSCSYZLRMLQSYLJXBCXTLWDQZPCYCYK"
+ "PPPNSXFYZJJRCEMHSZMSXLXGLRWGCSTLRSXBZGBZGZTCPLUJLSLYLYMTXMTZPALZXPXJTJWTCYYZLBLXBZLQMYLXPGHDSLSSDMXM"
+ "BDZZSXWHAMLCZCPJMCNHJYSNSYGCHSKQMZZQDLLKABLWJXSFMOCDXJRRLYQZKJMYBYQLYHETFJZFRFKSRYXFJTWDSXXSYSQJYSLY"
+ "XWJHSNLXYYXHBHAWHHJZXWMYLJCSSLKYDZTXBZSYFDXGXZJKHSXXYBSSXDPYNZWRPTQZCZENYGCXQFJYKJBZMLJCMQQXUOXSLYXX"
+ "LYLLJDZBTYMHPFSTTQQWLHOKYBLZZALZXQLHZWRRQHLSTMYPYXJJXMQSJFNBXYXYJXXYQYLTHYLQYFMLKLJTMLLHSZWKZHLJMLHL"
+ "JKLJSTLQXYLMBHHLNLZXQJHXCFXXLHYHJJGBYZZKBXSCQDJQDSUJZYYHZHHMGSXCSYMXFEBCQWWRBPYYJQTYZCYQYQQZYHMWFFHG"
+ "ZFRJFCDPXNTQYZPDYKHJLFRZXPPXZDBBGZQSTLGDGYLCQMLCHHMFYWLZYXKJLYPQHSYWMQQGQZMLZJNSQXJQSYJYCBEHSXFSZPXZ"
+ "WFLLBCYYJDYTDTHWZSFJMQQYJLMQXXLLDTTKHHYBFPWTYYSQQWNQWLGWDEBZWCMYGCULKJXTMXMYJSXHYBRWFYMWFRXYQMXYSZTZ"
+ "ZTFYKMLDHQDXWYYNLCRYJBLPSXCXYWLSPRRJWXHQYPHTYDNXHHMMYWYTZCSQMTSSCCDALWZTCPQPYJLLQZYJSWXMZZMMYLMXCLMX"
+ "CZMXMZSQTZPPQQBLPGXQZHFLJJHYTJSRXWZXSCCDLXTYJDCQJXSLQYCLZXLZZXMXQRJMHRHZJBHMFLJLMLCLQNLDXZLLLPYPSYJY"
+ "SXCQQDCMQJZZXHNPNXZMEKMXHYKYQLXSXTXJYYHWDCWDZHQYYBGYBCYSCFGPSJNZDYZZJZXRZRQJJYMCANYRJTLDPPYZBSTJKXXZ"
+ "YPFDWFGZZRPYMTNGXZQBYXNBUFNQKRJQZMJEGRZGYCLKXZDSKKNSXKCLJSPJYYZLQQJYBZSSQLLLKJXTBKTYLCCDDBLSPPFYLGYD"
+ "TZJYQGGKQTTFZXBDKTYYHYBBFYTYYBCLPDYTGDHRYRNJSPTCSNYJQHKLLLZSLYDXXWBCJQSPXBPJZJCJDZFFXXBRMLAZHCSNDLBJ"
+ "DSZBLPRZTSWSBXBCLLXXLZDJZSJPYLYXXYFTFFFBHJJXGBYXJPMMMPSSJZJMTLYZJXSWXTYLEDQPJMYGQZJGDJLQJWJQLLSJGJGY"
+ "GMSCLJJXDTYGJQJQJCJZCJGDZZSXQGSJGGCXHQXSNQLZZBXHSGZXCXYLJXYXYYDFQQJHJFXDHCTXJYRXYSQTJXYEFYYSSYYJXNCY"
+ "ZXFXMSYSZXYYSCHSHXZZZGZZZGFJDLTYLNPZGYJYZYYQZPBXQBDZTZCZYXXYHHSQXSHDHGQHJHGYWSZTMZMLHYXGEBTYLZKQWYTJ"
+ "ZRCLEKYSTDBCYKQQSAYXCJXWWGSBHJYZYDHCSJKQCXSWXFLTYNYZPZCCZJQTZWJQDZZZQZLJJXLSBHPYXXPSXSHHEZTXFPTLQYZZ"
+ "XHYTXNCFZYYHXGNXMYWXTZSJPTHHGYMXMXQZXTSBCZYJYXXTYYZYPCQLMMSZMJZZLLZXGXZAAJZYXJMZXWDXZSXZDZXLEYJJZQBH"
+ "ZWZZZQTZPSXZTDSXJJJZNYAZPHXYYSRNQDTHZHYYKYJHDZXZLSWCLYBZYECWCYCRYLCXNHZYDZYDYJDFRJJHTRSQTXYXJRJHOJYN"
+ "XELXSFSFJZGHPZSXZSZDZCQZBYYKLSGSJHCZSHDGQGXYZGXCHXZJWYQWGYHKSSEQZZNDZFKWYSSTCLZSTSYMCDHJXXYWEYXCZAYD"
+ "MPXMDSXYBSQMJMZJMTZQLPJYQZCGQHXJHHLXXHLHDLDJQCLDWBSXFZZYYSCHTYTYYBHECXHYKGJPXHHYZJFXHWHBDZFYZBCAPNPG"
+ "NYDMSXHMMMMAMYNBYJTMPXYYMCTHJBZYFCGTYHWPHFTWZZEZSBZEGPFMTSKFTYCMHFLLHGPZJXZJGZJYXZSBBQSCZZLZCCSTPGXM"
+ "JSFTCCZJZDJXCYBZLFCJSYZFGSZLYBCWZZBYZDZYPSWYJZXZBDSYUXLZZBZFYGCZXBZHZFTPBGZGEJBSTGKDMFHYZZJHZLLZZGJQ"
+ "ZLSFDJSSCBZGPDLFZFZSZYZYZSYGCXSNXXCHCZXTZZLJFZGQSQYXZJQDCCZTQCDXZJYQJQCHXZTDLGSCXZSYQJQTZWLQDQZTQCHQ"
+ "QJZYEZZZPBWKDJFCJPZTYPQYQTTYNLMBDKTJZPQZQZZFPZSBNJLGYJDXJDZZKZGQKXDLPZJTCJDQBXDJQJSTCKNXBXZMSLYJCQMT"
+ "JQWWCJQNJNLLLHJCWQTBZQYDZCZPZZDZYDDCYZZZCCJTTJFZDPRRTZTJDCQTQZDTJNPLZBCLLCTZSXKJZQZPZLBZRBTJDCXFCZDB"
+ "CCJJLTQQPLDCGZDBBZJCQDCJWYNLLZYZCCDWLLXWZLXRXNTQQCZXKQLSGDFQTDDGLRLAJJTKUYMKQLLTZYTDYYCZGJWYXDXFRSKS"
+ "TQTENQMRKQZHHQKDLDAZFKYPBGGPZREBZZYKZZSPEGJXGYKQZZZSLYSYYYZWFQZYLZZLZHWCHKYPQGNPGBLPLRRJYXCCSYYHSFZF"
+ "YBZYYTGZXYLXCZWXXZJZBLFFLGSKHYJZEYJHLPLLLLCZGXDRZELRHGKLZZYHZLYQSZZJZQLJZFLNBHGWLCZCFJYSPYXZLZLXGCCP"
+ "ZBLLCYBBBBUBBCBPCRNNZCZYRBFSRLDCGQYYQXYGMQZWTZYTYJXYFWTEHZZJYWLCCNTZYJJZDEDPZDZTSYQJHDYMBJNYJZLXTSST"
+ "PHNDJXXBYXQTZQDDTJTDYYTGWSCSZQFLSHLGLBCZPHDLYZJYCKWTYTYLBNYTSDSYCCTYSZYYEBHEXHQDTWNYGYCLXTSZYSTQMYGZ"
+ "AZCCSZZDSLZCLZRQXYYELJSBYMXSXZTEMBBLLYYLLYTDQYSHYMRQWKFKBFXNXSBYCHXBWJYHTQBPBSBWDZYLKGZSKYHXQZJXHXJX"
+ "GNLJKZLYYCDXLFYFGHLJGJYBXQLYBXQPQGZTZPLNCYPXDJYQYDYMRBESJYYHKXXSTMXRCZZYWXYQYBMCLLYZHQYZWQXDBXBZWZMS"
+ "LPDMYSKFMZKLZCYQYCZLQXFZZYDQZPZYGYJYZMZXDZFYFYTTQTZHGSPCZMLCCYTZXJCYTJMKSLPZHYSNZLLYTPZCTZZCKTXDHXXT"
+ "QCYFKSMQCCYYAZHTJPCYLZLYJBJXTPNYLJYYNRXSYLMMNXJSMYBCSYSYLZYLXJJQYLDZLPQBFZZBLFNDXQKCZFYWHGQMRDSXYCYT"
+ "XNQQJZYYPFZXDYZFPRXEJDGYQBXRCNFYYQPGHYJDYZXGRHTKYLNWDZNTSMPKLBTHBPYSZBZTJZSZZJTYYXZPHSSZZBZCZPTQFZMY"
+ "FLYPYBBJQXZMXXDJMTSYSKKBJZXHJCKLPSMKYJZCXTMLJYXRZZQSLXXQPYZXMKYXXXJCLJPRMYYGADYSKQLSNDHYZKQXZYZTCGHZ"
+ "TLMLWZYBWSYCTBHJHJFCWZTXWYTKZLXQSHLYJZJXTMPLPYCGLTBZZTLZJCYJGDTCLKLPLLQPJMZPAPXYZLKKTKDZCZZBNZDYDYQZ"
+ "JYJGMCTXLTGXSZLMLHBGLKFWNWZHDXUHLFMKYSLGXDTWWFRJEJZTZHYDXYKSHWFZCQSHKTMQQHTZHYMJDJSKHXZJZBZZXYMPAGQM"
+ "STPXLSKLZYNWRTSQLSZBPSPSGZWYHTLKSSSWHZZLYYTNXJGMJSZSUFWNLSOZTXGXLSAMMLBWLDSZYLAKQCQCTMYCFJBSLXCLZZCL"
+ "XXKSBZQCLHJPSQPLSXXCKSLNHPSFQQYTXYJZLQLDXZQJZDYYDJNZPTUZDSKJFSLJHYLZSQZLBTXYDGTQFDBYAZXDZHZJNHHQBYKN"
+ "XJJQCZMLLJZKSPLDYCLBBLXKLELXJLBQYCXJXGCNLCQPLZLZYJTZLJGYZDZPLTQCSXFDMNYCXGBTJDCZNBGBQYQJWGKFHTNPYQZQ"
+ "GBKPBBYZMTJDYTBLSQMPSXTBNPDXKLEMYYCJYNZCTLDYKZZXDDXHQSHDGMZSJYCCTAYRZLPYLTLKXSLZCGGEXCLFXLKJRTLQJAQZ"
+ "NCMBYDKKCXGLCZJZXJHPTDJJMZQYKQSECQZDSHHADMLZFMMZBGNTJNNLGBYJBRBTMLBYJDZXLCJLPLDLPCQDHLXZLYCBLCXZZJAD"
+ "JLNZMMSSSMYBHBSQKBHRSXXJMXSDZNZPXLGBRHWGGFCXGMSKLLTSJYYCQLTSKYWYYHYWXBXQYWPYWYKQLSQPTNTKHQCWDQKTWPXX"
+ "HCPTHTWUMSSYHBWCRWXHJMKMZNGWTMLKFGHKJYLSYYCXWHYECLQHKQHTTQKHFZLDXQWYZYYDESBPKYRZPJFYYZJCEQDZZDLATZBB"
+ "FJLLCXDLMJSSXEGYGSJQXCWBXSSZPDYZCXDNYXPPZYDLYJCZPLTXLSXYZYRXCYYYDYLWWNZSAHJSYQYHGYWWAXTJZDAXYSRLTDPS"
+ "SYYFNEJDXYZHLXLLLZQZSJNYQYQQXYJGHZGZCYJCHZLYCDSHWSHJZYJXCLLNXZJJYYXNFXMWFPYLCYLLABWDDHWDXJMCXZTZPMLQ"
+ "ZHSFHZYNZTLLDYWLSLXHYMMYLMBWWKYXYADTXYLLDJPYBPWUXJMWMLLSAFDLLYFLBHHHBQQLTZJCQJLDJTFFKMMMBYTHYGDCQRDD"
+ "WRQJXNBYSNWZDBYYTBJHPYBYTTJXAAHGQDQTMYSTQXKBTZPKJLZRBEQQSSMJJBDJOTGTBXPGBKTLHQXJJJCTHXQDWJLWRFWQGWSH"
+ "CKRYSWGFTGYGBXSDWDWRFHWYTJJXXXJYZYSLPYYYPAYXHYDQKXSHXYXGSKQHYWFDDDPPLCJLQQEEWXKSYYKDYPLTJTHKJLTCYYHH"
+ "JTTPLTZZCDLTHQKZXQYSTEEYWYYZYXXYYSTTJKLLPZMCYHQGXYHSRMBXPLLNQYDQHXSXXWGDQBSHYLLPJJJTHYJKYPPTHYYKTYEZ"
+ "YENMDSHLCRPQFDGFXZPSFTLJXXJBSWYYSKSFLXLPPLBBBLBSFXFYZBSJSSYLPBBFFFFSSCJDSTZSXZRYYSYFFSYZYZBJTBCTSBSD"
+ "HRTJJBYTCXYJEYLXCBNEBJDSYXYKGSJZBXBYTFZWGENYHHTHZHHXFWGCSTBGXKLSXYWMTMBYXJSTZSCDYQRCYTWXZFHMYMCXLZNS"
+ "DJTTTXRYCFYJSBSDYERXJLJXBBDEYNJGHXGCKGSCYMBLXJMSZNSKGXFBNBPTHFJAAFXYXFPXMYPQDTZCXZZPXRSYWZDLYBBKTYQP"
+ "QJPZYPZJZNJPZJLZZFYSBTTSLMPTZRTDXQSJEHBZYLZDHLJSQMLHTXTJECXSLZZSPKTLZKQQYFSYGYWPCPQFHQHYTQXZKRSGTTSQ"
+ "CZLPTXCDYYZXSQZSLXLZMYCPCQBZYXHBSXLZDLTCDXTYLZJYYZPZYZLTXJSJXHLPMYTXCQRBLZSSFJZZTNJYTXMYJHLHPPLCYXQJ"
+ "QQKZZSCPZKSWALQSBLCCZJSXGWWWYGYKTJBBZTDKHXHKGTGPBKQYSLPXPJCKBMLLXDZSTBKLGGQKQLSBKKTFXRMDKBFTPZFRTBBR"
+ "FERQGXYJPZSSTLBZTPSZQZSJDHLJQLZBPMSMMSXLQQNHKNBLRDDNXXDHDDJCYYGYLXGZLXSYGMQQGKHBPMXYXLYTQWLWGCPBMQXC"
+ "YZYDRJBHTDJYHQSHTMJSBYPLWHLZFFNYPMHXXHPLTBQPFBJWQDBYGPNZTPFZJGSDDTQSHZEAWZZYLLTYYBWJKXXGHLFKXDJTMSZS"
+ "QYNZGGSWQSPHTLSSKMCLZXYSZQZXNCJDQGZDLFNYKLJCJLLZLMZZNHYDSSHTHZZLZZBBHQZWWYCRZHLYQQJBEYFXXXWHSRXWQHWP"
+ "SLMSSKZTTYGYQQWRSLALHMJTQJSMXQBJJZJXZYZKXBYQXBJXSHZTSFJLXMXZXFGHKZSZGGYLCLSARJYHSLLLMZXELGLXYDJYTLFB"
+ "HBPNLYZFBBHPTGJKWETZHKJJXZXXGLLJLSTGSHJJYQLQZFKCGNNDJSSZFDBCTWWSEQFHQJBSAQTGYPQLBXBMMYWXGSLZHGLZGQYF"
+ "LZBYFZJFRYSFMBYZHQGFWZSYFYJJPHZBYYZFFWODGRLMFTWLBZGYCQXCDJYGZYYYYTYTYDWEGAZYHXJLZYYHLRMGRXXZCLHNELJJ"
+ "TJTPWJYBJJBXJJTJTEEKHWSLJPLPSFYZPQQBDLQJJTYYQLYZKDKSQJYYQZLDQTGJQYZJSUCMRYQTHTEJMFCTYHYPKMHYZWJDQFHY"
+ "YXWSHCTXRLJHQXHCCYYYJLTKTTYTMXGTCJTZAYYOCZLYLBSZYWJYTSJYHBYSHFJLYGJXXTMZYYLTXXYPZLXYJZYZYYPNHMYMDYYL"
+ "BLHLSYYQQLLNJJYMSOYQBZGDLYXYLCQYXTSZEGXHZGLHWBLJHEYXTWQMAKBPQCGYSHHEGQCMWYYWLJYJHYYZLLJJYLHZYHMGSLJL"
+ "JXCJJYCLYCJPCPZJZJMMYLCQLNQLJQJSXYJMLSZLJQLYCMMHCFMMFPQQMFYLQMCFFQMMMMHMZNFHHJGTTHHKHSLNCHHYQDXTMMQD"
+ "CYZYXYQMYQYLTDCYYYZAZZCYMZYDLZFFFMMYCQZWZZMABTBYZTDMNZZGGDFTYPCGQYTTSSFFWFDTZQSSYSTWXJHXYTSXXYLBYQHW"
+ "WKXHZXWZNNZZJZJJQJCCCHYYXBZXZCYZTLLCQXYNJYCYYCYNZZQYYYEWYCZDCJYCCHYJLBTZYYCQWMPWPYMLGKDLDLGKQQBGYCHJ"
+ "XY";

        /// <summary>
        /// 获取传入中文的首字母，如果是英文直接返回
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static string GetChineseSpell(string strText)
        {
            if (strText == null || strText.Length == 0)
                return strText;
            System.Text.StringBuilder myStr = new System.Text.StringBuilder();
            foreach (char vChar in strText)
            {
                // 若是字母则直接输出
                if ((vChar >= 'a' && vChar <= 'z') || (vChar >= 'A' && vChar <= 'Z'))
                    myStr.Append(char.ToUpper(vChar));
                else if ((int)vChar >= 19968 && (int)vChar <= 40869)
                {
                    // 对可以查找的汉字计算它的首拼音字母的位置，然后输出
                    myStr.Append(strChineseFirstPY[(int)vChar - 19968]);
                }
            }
            return myStr.ToString();
        }


        

    }












}
