using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using Dapper;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberCareGiverRepository : IMemberCareGiverRepository
    {
        private readonly string _connectionString;
        public MemberCareGiverRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        private NpgsqlConnection Open() => new NpgsqlConnection(_connectionString);
        public async Task<IReadOnlyList<MemberCaregiverDto>> GetBundleByMemberDetailsIdAsync(int memberDetailsId)
        {
            const string sql = @"
                -- 1) caregivers -> maps to MemberCaregiver
                -- 1) caregivers -> MemberCaregiver
            SELECT
                mcg.membercaregiverid         AS MemberCaregiverId,
                mcg.memberdetailsid           AS MemberDetailsId,
                mcg.caregiverfirstname        AS CaregiverFirstName,
                mcg.caregiverlastname         AS CaregiverLastName,
                mcg.caregivermiddlename       AS CaregiverMiddleName,
                mcg.caregiverbrithdate        AS CaregiverBrithDate,
                mcg.genderid                  AS GenderId,
                mcg.ethnicityid               AS EthnicityId,
                mcg.raceid                    AS RaceId,
                mcg.residencestatusid         AS ResidenceStatusId,
                mcg.maritalstatusid           AS MaritalStatusId,
                mcg.relationshiptypeid        AS RelationshipTypeId,
                mcg.primaryemail              AS PrimaryEmail,
                mcg.alternateemail            AS AlternateEmail,
                CASE WHEN mcg.ishealthcareproxy = B'1' THEN true WHEN mcg.ishealthcareproxy = B'0' THEN false ELSE NULL END AS IsHealthcareProxy,
                CASE WHEN mcg.isprimary        = B'1' THEN true WHEN mcg.isprimary        = B'0' THEN false ELSE NULL END AS IsPrimary,
                CASE WHEN mcg.isformalcaregiver= B'1' THEN true WHEN mcg.isformalcaregiver= B'0' THEN false ELSE NULL END AS IsFormalCaregiver,
                COALESCE(mcg.activeflag, true) AS ActiveFlag,
                mcg.createdon                 AS CreatedOn,
                mcg.createdby                 AS CreatedBy,
                mcg.updatedon                 AS UpdatedOn,
                mcg.updatedby                 AS UpdatedBy,
                mcg.deletedon                 AS DeletedOn,
                mcg.deletedby                 AS DeletedBy
            FROM public.membercaregiver mcg
            WHERE mcg.memberdetailsid = @memberDetailsId
              AND COALESCE(mcg.activeflag, true) = true
            ORDER BY mcg.membercaregiverid;

            -- 2) addresses -> MemberCaregiverAddress
            SELECT
                a.membercaregiveraddressid  AS MemberCaregiverAddressId,
                a.membercaregiverid         AS MemberCaregiverId,
                a.addresstypeid             AS AddressTypeId,
                a.addressline1              AS AddressLine1,
                a.addressline2              AS AddressLine2,
                a.addressline3              AS AddressLine3,
                a.city                      AS City,
                a.countyid                  AS CountyId,
                a.stateid                   AS StateId,
                a.country                   AS Country,
                a.zipcode                   AS ZipCode,
                a.boroughid                 AS BoroughId,
                a.islandid                  AS IslandId,
                a.regionid                  AS RegionId,
                CASE WHEN a.isprimary = B'1' THEN true WHEN a.isprimary = B'0' THEN false ELSE NULL END AS IsPrimary,
                COALESCE(a.activeflag, true) AS ActiveFlag,
                a.createdon                 AS CreatedOn,
                a.createdby                 AS CreatedBy,
                a.updatedon                 AS UpdatedOn,
                a.updatedby                 AS UpdatedBy,
                a.deletedon                 AS DeletedOn,
                a.deletedby                 AS DeletedBy
            FROM public.membercaregiveraddress a
            JOIN public.membercaregiver mcg ON mcg.membercaregiverid = a.membercaregiverid
            WHERE mcg.memberdetailsid = @memberDetailsId
              AND COALESCE(a.activeflag, true) = true;

            -- 3) phones -> MemberCaregiverPhone
            SELECT
                p.membercaregiverphoneid    AS MemberCaregiverPhoneId,
                p.membercaregiverid         AS MemberCaregiverId,
                p.phonetypeid               AS PhoneTypeId,
                ''               AS PhoneNumber,
                ''                 AS Extension,
                CASE WHEN p.isprimary = B'1' THEN true WHEN p.isprimary = B'0' THEN false ELSE NULL END AS IsPrimary,
                COALESCE(p.activeflag, true) AS ActiveFlag,
                p.createdon                 AS CreatedOn,
                p.createdby                 AS CreatedBy,
                p.updatedon                 AS UpdatedOn,
                p.updatedby                 AS UpdatedBy,
                p.deletedon                 AS DeletedOn,
                p.deletedby                 AS DeletedBy
            FROM public.membercaregiverphone p
            JOIN public.membercaregiver mcg ON mcg.membercaregiverid = p.membercaregiverid
            WHERE mcg.memberdetailsid = @memberDetailsId
              AND COALESCE(p.activeflag, true) = true;

            -- 4) languages -> MemberCaregiverLanguage
            SELECT
                l.membercaregiverlanguageid AS MemberCaregiverLanguageId,
                l.membercaregiverid         AS MemberCaregiverId,
                l.languageid                AS LanguageId,
                CASE WHEN l.isprimary = B'1' THEN true WHEN l.isprimary = B'0' THEN false ELSE NULL END AS IsPrimary,
                COALESCE(l.activeflag, true) AS ActiveFlag,
                l.createdon                 AS CreatedOn,
                l.createdby                 AS CreatedBy,
                l.updatedon                 AS UpdatedOn,
                l.updatedby                 AS UpdatedBy,
                l.deletedon                 AS DeletedOn,
                l.deletedby                 AS DeletedBy
            FROM public.membercaregiverlanguage l
            JOIN public.membercaregiver mcg ON mcg.membercaregiverid = l.membercaregiverid
            WHERE mcg.memberdetailsid = @memberDetailsId
              AND COALESCE(l.activeflag, true) = true;

            -- 5) portal -> MemberCaregiverMemberPortal
            SELECT
                pa.membercaregivermemberportalid AS MemberCaregiverMemberPortalId,
                pa.membercaregiverid             AS MemberCaregiverId,
                CASE WHEN pa.ismemberportalaccess    = B'1' THEN true WHEN pa.ismemberportalaccess    = B'0' THEN false ELSE NULL END AS IsMemberPortalAccess,
                CASE WHEN pa.isregistrationrequired  = B'1' THEN true WHEN pa.isregistrationrequired  = B'0' THEN false ELSE NULL END AS IsRegistrationRequired,
                COALESCE(pa.activeflag, true)        AS ActiveFlag,
                pa.createdon                     AS CreatedOn,
                pa.createdby                     AS CreatedBy,
                pa.updatedon                     AS UpdatedOn,
                pa.updatedby                     AS UpdatedBy,
                pa.deletedon                     AS DeletedOn,
                pa.deletedby                     AS DeletedBy
            FROM public.membercaregivermemberportal pa
            JOIN public.membercaregiver mcg ON mcg.membercaregiverid = pa.membercaregiverid
            WHERE mcg.memberdetailsid = @memberDetailsId
              AND COALESCE(pa.activeflag, true) = true;

                ";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var grid = await conn.QueryMultipleAsync(sql, new { memberDetailsId });

            // 1) caregivers (root)
            var caregivers = (await grid.ReadAsync<MemberCaregiver>()).ToList();
            if (caregivers.Count == 0) return Array.Empty<MemberCaregiverDto>();

            // Build DTOs indexed by CaregiverId
            var map = caregivers.ToDictionary(
                c => c.MemberCaregiverId,
                c => new MemberCaregiverDto { Caregiver = c }
            );

            // 2) addresses
            foreach (var a in await grid.ReadAsync<MemberCaregiverAddress>())
                if (a.MemberCaregiverId.HasValue && map.TryGetValue(a.MemberCaregiverId.Value, out var dto))
                    dto.Addresses.Add(a);

            // 3) phones
            foreach (var p in await grid.ReadAsync<MemberCaregiverPhone>())
                if (p.MemberCaregiverId.HasValue && map.TryGetValue(p.MemberCaregiverId.Value, out var dto))
                    dto.Phones.Add(p);

            // 4) languages
            foreach (var l in await grid.ReadAsync<MemberCaregiverLanguage>())
                if (l.MemberCaregiverId.HasValue && map.TryGetValue(l.MemberCaregiverId.Value, out var dto))
                    dto.Languages.Add(l);

            // 5) portal
            foreach (var portal in await grid.ReadAsync<MemberCaregiverMemberPortal>())
                if (portal.MemberCaregiverId.HasValue && map.TryGetValue(portal.MemberCaregiverId.Value, out var dto))
                    dto.Portal.Add(portal);

            return map.Values.ToList();
        }



        public async Task<int> CreateAsync(MemberCaregiver m)
        {
            const string sql = @"
INSERT INTO public.membercaregiver
(
    memberdetailsid,
    caregiverfirstname, caregiverlastname, caregivermiddlename, caregiverbrithdate,
    genderid, ethnicityid, raceid, residencestatusid, maritalstatusid, relationshiptypeid,
    primaryemail, alternateemail,
    ishealthcareproxy, isprimary, isformalcaregiver,
    activeflag, createdon, createdby, updatedon, updatedby
)
VALUES
(
    @MemberDetailsId,
    @CaregiverFirstName, @CaregiverLastName, @CaregiverMiddleName, @CaregiverBrithDate,
    @GenderId, @EthnicityId, @RaceId, @ResidenceStatusId, @MaritalStatusId, @RelationshipTypeId,
    @PrimaryEmail, @AlternateEmail,
    CASE WHEN @IsHealthcareProxy THEN B'1' ELSE B'0' END,
    CASE WHEN @IsPrimary THEN B'1' ELSE B'0' END,
    CASE WHEN @IsFormalCaregiver THEN B'1' ELSE B'0' END,
    COALESCE(@ActiveFlag, true),
    COALESCE(@CreatedOn, NOW()), @CreatedBy, @UpdatedOn, @UpdatedBy
)
RETURNING membercaregiverid;";

            using var conn = Open();
            var id = await conn.ExecuteScalarAsync<int>(sql, m);
            return id;
        }

        public async Task<MemberCaregiver?> GetByIdAsync(int memberCaregiverId)
        {
            const string sql = @"
SELECT
    membercaregiverid AS MemberCaregiverId,
    memberdetailsid AS MemberDetailsId,
    caregiverfirstname AS CaregiverFirstName,
    caregiverlastname AS CaregiverLastName,
    caregivermiddlename AS CaregiverMiddleName,
    caregiverbrithdate AS CaregiverBrithDate,
    genderid AS GenderId, ethnicityid AS EthnicityId, raceid AS RaceId,
    residencestatusid AS ResidenceStatusId, maritalstatusid AS MaritalStatusId, relationshiptypeid AS RelationshipTypeId,
    primaryemail AS PrimaryEmail, alternateemail AS AlternateEmail,
    CASE WHEN ishealthcareproxy = B'1' THEN true WHEN ishealthcareproxy = B'0' THEN false ELSE NULL END AS IsHealthcareProxy,
    CASE WHEN isprimary = B'1' THEN true WHEN isprimary = B'0' THEN false ELSE NULL END AS IsPrimary,
    CASE WHEN isformalcaregiver = B'1' THEN true WHEN isformalcaregiver = B'0' THEN false ELSE NULL END AS IsFormalCaregiver,
    activeflag AS ActiveFlag,
    createdon AS CreatedOn, createdby AS CreatedBy, updatedon AS UpdatedOn, updatedby AS UpdatedBy,
    deletedon AS DeletedOn, deletedby AS DeletedBy
FROM public.membercaregiver
WHERE membercaregiverid = @memberCaregiverId;";

            using var conn = Open();
            return await conn.QueryFirstOrDefaultAsync<MemberCaregiver>(sql, new { memberCaregiverId });
        }

        public async Task<bool> UpdateAsync(MemberCaregiver m)
        {
            const string sql = @"
UPDATE public.membercaregiver
SET
    memberdetailsid = @MemberDetailsId,
    caregiverfirstname = @CaregiverFirstName,
    caregiverlastname = @CaregiverLastName,
    caregivermiddlename = @CaregiverMiddleName,
    caregiverbrithdate = @CaregiverBrithDate,
    genderid = @GenderId,
    ethnicityid = @EthnicityId,
    raceid = @RaceId,
    residencestatusid = @ResidenceStatusId,
    maritalstatusid = @MaritalStatusId,
    relationshiptypeid = @RelationshipTypeId,
    primaryemail = @PrimaryEmail,
    alternateemail = @AlternateEmail,
    ishealthcareproxy = CASE WHEN @IsHealthcareProxy THEN B'1' ELSE B'0' END,
    isprimary = CASE WHEN @IsPrimary THEN B'1' ELSE B'0' END,
    isformalcaregiver = CASE WHEN @IsFormalCaregiver THEN B'1' ELSE B'0' END,
    activeflag = COALESCE(@ActiveFlag, activeflag),
    updatedon = COALESCE(@UpdatedOn, NOW()),
    updatedby = @UpdatedBy
WHERE membercaregiverid = @MemberCaregiverId;";

            using var conn = Open();
            var rows = await conn.ExecuteAsync(sql, m);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int memberCaregiverId, int deletedBy)
        {
            const string sql = @"
UPDATE public.membercaregiver
SET activeflag = false,
    deletedon = NOW(),
    deletedby = @deletedBy
WHERE membercaregiverid = @memberCaregiverId;";

            using var conn = Open();
            var rows = await conn.ExecuteAsync(sql, new { memberCaregiverId, deletedBy });
            return rows > 0;
        }
    }
}
