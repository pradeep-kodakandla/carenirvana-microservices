using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class codesets
    {
        public int? Id { get; set; }
        public string code { get; set; }
        public string codeDesc { get; set; }
        public string codeShortDesc { get; set; }
        public DateTime? effectiveDate { get; set; }
        public DateTime? endDate { get; set; }
        public string severity { get; set; }
        public string laterality { get; set; }
        public string activeFlag { get; set; }
        public string type { get; set; }
    }

    public sealed class CodeSearchResult
    {
        public int? Id { get; set; }
        public string? code { get; set; }
        public string? codeDesc { get; set; }
        public string? codeShortDesc { get; set; }
        public string type { get; set; } = "ICD";
    }

    public sealed class MemberSearchResult
    {
        public int memberdetailsid { get; set; }
        public string? memberid { get; set; }
        public string? firstname { get; set; }
        public string? lastname { get; set; }
        public string? birthdate { get; set; }   // keep as formatted string for UI
        public string? city { get; set; }
        public string? phone { get; set; }
        public string? gender { get; set; }
    }

    public sealed class MedicationSearchResult
    {
        public string? drugName { get; set; }
        public string? ndc { get; set; }
    }

    public sealed class StaffSearchResult
    {
        public int userdetailid { get; set; }
        public string? username { get; set; }

        // Added
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? email { get; set; }
        public string? role { get; set; }
        public string? fullName { get; set; }
    }

    public sealed class ProviderSearchResult
    {
        public int providerId { get; set; }
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public string? fullName { get; set; }
        public string? email { get; set; }
        public string? npi { get; set; }
        public string? organizationName { get; set; }

        // flattened primary contact/address for template fill
        public string? phone { get; set; }
        public string? fax { get; set; }
        public string? taxId { get; set; }
        public string? addressLine1 { get; set; }
        public string? addressLine2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }    // keep as string (stateid is int in your query; map how you like)
        public string? zipCode { get; set; }
    }

    // matches your GET Provider query (json_agg blocks)
    public sealed class ProviderDetailResult
    {
        public int providerId { get; set; }
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public string? fullName { get; set; }
        public string? email { get; set; }
        public string? npi { get; set; }
        public string? organizationName { get; set; }

        public string? telecomInfoJson { get; set; }     // json_agg telecom_info
        public string? addressesJson { get; set; }       // json_agg addresses
        public string? licensesJson { get; set; }
        public string? boardCertificationsJson { get; set; }
        public string? educationJson { get; set; }
        public string? identifiersJson { get; set; }
        public string? languagesJson { get; set; }
        public string? networksJson { get; set; }
        public string? credentialingJson { get; set; }
        public string? liabilityInsuranceJson { get; set; }
        public string? accreditationsJson { get; set; }
        public string? rolesJson { get; set; }

        public bool? active { get; set; }
        public bool? acceptingNewPatients { get; set; }
        public DateTime? createdOn { get; set; }
        public DateTime? updatedOn { get; set; }
    }

    public sealed class ClaimSearchResult
    {
        public long memberClaimHeaderId { get; set; }
        public int? memberDetailsId { get; set; }
        public string? claimNumber { get; set; }
        public int? providerId { get; set; }
        public string? providerName { get; set; }
        public DateTime? dosFrom { get; set; }
        public DateTime? dosTo { get; set; }
        public int? visitTypeId { get; set; }
        public string? reasonForVisit { get; set; }

        // totals for your template fill fields (billed/allowed/copay/paid)
        public decimal? billed { get; set; }
        public decimal? allowedAmount { get; set; }
        public decimal? copayAmount { get; set; }
        public decimal? paid { get; set; }
    }

    // matches your "Retrieve single selected claim" query (+ totals added in repo SQL below)
    public sealed class ClaimDetailResult
    {
        public long memberClaimHeaderId { get; set; }
        public int? memberDetailsId { get; set; }
        public string? claimNumber { get; set; }
        public int? claimTypeId { get; set; }
        public string? billType { get; set; }
        public int? providerId { get; set; }
        public int? enrollmentHierarchyId { get; set; }
        public string? companyCode { get; set; }
        public string? patContrrolNumber { get; set; }
        public string? authNumber { get; set; }
        public int? visitTypeId { get; set; }
        public string? reasonForVisit { get; set; }
        public DateTime? dosFrom { get; set; }
        public DateTime? dosTo { get; set; }
        public int? los { get; set; }
        public int? placeOfServiceId { get; set; }
        public string? medicalRecordNumber { get; set; }
        public string? programType { get; set; }
        public int? claimStatusId { get; set; }
        public int? holdCodeId { get; set; }
        public string? notes { get; set; }
        public DateTime? receivedDate { get; set; }
        public DateTime? paidDate { get; set; }
        public DateTime? checkDate { get; set; }
        public string? checkNumber { get; set; }
        public bool? isSensitive { get; set; }

        public decimal? billed { get; set; }
        public decimal? allowedAmount { get; set; }
        public decimal? copayAmount { get; set; }
        public decimal? paid { get; set; }

        public string? claimLinesJson { get; set; }
        public string? diagnosesJson { get; set; }
        public string? paymentsJson { get; set; }
        public string? documentsJson { get; set; }

        public DateTime? createdOn { get; set; }
        public string? createdBy { get; set; }
        public DateTime? updatedOn { get; set; }
        public string? updatedBy { get; set; }
    }
}