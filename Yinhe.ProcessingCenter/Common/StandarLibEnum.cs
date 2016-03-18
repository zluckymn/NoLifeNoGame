using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter.Common
{
    /// <summary>
    /// 标准成果库枚举
    /// </summary>
    public enum StandarLibEnum
    {
        /// <summary>
        /// 户型库
        /// </summary>
        [EnumDescription("户型库")]
        UNITLIB=1,
        /// <summary>
        /// 室内精装修
        /// </summary>
        [EnumDescription("室内精装修")]
        DECORATIONLIB=2, 
        /// <summary>
        /// 立面库
        /// </summary>
        [EnumDescription("立面库")]
        FACADELIB=3, 
        /// <summary>
        /// 示范区库
        /// </summary>
        [EnumDescription("示范区库")]
        DEMAREALIB=4, 
        /// <summary>
        /// 景观库
        /// </summary>
        [EnumDescription("景观库")]
        LANDSCAPELIB=5,
        /// <summary>
        /// 标准工艺工法库
        /// </summary>
        [EnumDescription("标准工艺工法库")]
        CRAFTSLIB=6,
        /// <summary>
        /// 公共部位库
        /// </summary>
        [EnumDescription("公共部位库")]
        PARTSLIB=7,
        /// <summary>
        /// 设备与技术库 
        /// </summary>
        [EnumDescription("设备与技术库")]
        DEVICELIB=8
    }
}
