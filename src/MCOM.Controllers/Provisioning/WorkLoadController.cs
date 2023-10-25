using System.Data;
using System.Linq;
using MCOM.Models.Provisioning;

namespace MCOM.Controllers.Provisioning
{
    public class WorkLoadController
    {
        public static IEnumerable<WorkLoad> GetWorkLoads(DataTable dt)
        {
            return dt.Rows.Cast<DataRow>().Select(dr => new WorkLoad()
            {
                Id = Convert.ToInt32(dr["workload_id"]),
                Name = dr["workload_name"].ToString(),
                Description = dr["workload_description"].ToString(),
                Active = Convert.ToBoolean(dr["workload_active"])
            });
        }
    }
}
