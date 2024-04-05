using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Database_Entities;
using Models.FormModels;

namespace Models
{
    public class EditUserInfoModel
    {
        public UserEntity User { get; set; }
        public ThreeStringsModel Strings { get; set; }
    }
}
