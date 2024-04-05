using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Models.Database_Entities;

namespace Models
{
    public class UploadModel
    {
        public IFormFile ExcelFile { get; set; }
        public DeckEntity Deck { get; set; }
    }
}
