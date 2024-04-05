using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Interfaces.DatabaseAccess
{
    public interface IExecuteSQLquery
    {
        Task<int> ExecuteQueryAsync_ReturnInt<GenericParameters>(GenericParameters parameters, string sqlQuery);
        Task<Model> ExecuteQueryAsync_ReturnDTO<Model, GenericParameters>(GenericParameters parameters, string sqlQuery);
        Task<List<ModelList>> ExecuteQueryAsync_ReturnDTOlist<ModelList, GenericParameters>(GenericParameters parameters, string sqlQuery);
    }
}
