using CareNirvana.DataAccess;
using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class ConfigRepository(IAbstractDataLayer dataLayer)
    {
        private readonly IAbstractDataLayer dataLayer = dataLayer;

        // get User Types
        public IList<CfgUserType> GetUserType()
        {
            var sql = "select * from cfgusertype where activeflag=1";
            using var reader = dataLayer.ExecuteDataReader(sql);

            var userTypes = new List<CfgUserType>();
            while (reader.Read())
            {
                userTypes.Add(
                    new CfgUserType
                    { 
                        UserTypeId = reader.GetInt32(0),
                        UserTypeName = reader.GetString(1),
                        ActiveFlag  =  reader.GetBoolean(2)
                    });
            }
            return userTypes;
        }

        // add User Type
        public CfgUserType AddUserType(CfgUserType userType)
        {
            return null;
        }

        // update User Type
        public CfgUserType UpdateUserType(CfgUserType userType)
        {
            return null;
        }

        // delete User Type
        public bool DeleteUserType(CfgUserType userType)
        {
            return false;
        }
    }
}
