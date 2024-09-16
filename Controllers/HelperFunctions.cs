using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialWelfare.Controllers.User;
using SocialWelfare.Models.Entities;

public class UserHelperFunctions
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly SocialWelfareDepartmentContext dbcontext;

    private readonly ILogger<UserHelperFunctions> _logger;
    public UserHelperFunctions(IWebHostEnvironment webHostEnvironment, SocialWelfareDepartmentContext dbcontext, ILogger<UserHelperFunctions> logger)
    {
        this.dbcontext = dbcontext;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }


    public async Task<string> GetFilePath(IFormFile? docFile)
    {
        string docPath = "";
        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        string shortGuid = Guid.NewGuid().ToString("N")[..8];
        string uniqueName = shortGuid + "_" + docFile?.FileName;

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        if (docFile != null && docFile.Length > 0)
        {
            string filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await docFile.CopyToAsync(stream);
            }

            docPath = "/uploads/" + uniqueName;
        }

        return docPath;
    }

    public string GetCurrentFinancialYear()
    {
        var today = DateTime.Today;
        int startYear;

        if (today.Month < 4)
        {
            startYear = today.Year - 1;
        }
        else
        {
            startYear = today.Year;
        }

        int endYear = startYear + 1;
        return $"{startYear}-{endYear}";
    }

    public string GenerateApplicationId(int districtId, SocialWelfareDepartmentContext dbcontext, ILogger<UserController> _logger)
    {
        string? districtShort = dbcontext.Districts.FirstOrDefault(u => u.DistrictId == districtId)?.DistrictShort;

        string financialYear = GetCurrentFinancialYear();

        var result = dbcontext.ApplicationPerDistricts.FirstOrDefault(a=>a.DistrictId==districtId && a.FinancialYear==financialYear);

        int countPerDistrict = result?.CountValue ?? 0;

        string sql = "";

        if (countPerDistrict != 0)
            sql = "UPDATE ApplicationPerDistrict SET CountValue = @CountValue WHERE DistrictId = @districtId AND FinancialYear = @financialyear";
        else
            sql = "INSERT INTO ApplicationPerDistrict (DistrictId, FinancialYear, CountValue) VALUES (@districtId, @financialyear, @CountValue)";

        countPerDistrict++; // Increment before using in SqlParameter

        dbcontext.Database.ExecuteSqlRaw(sql,
            new SqlParameter("@districtId", districtId),
            new SqlParameter("@financialyear", financialYear),
            new SqlParameter("@CountValue", countPerDistrict));

        return $"{districtShort}/{financialYear}/{countPerDistrict}";
    }


    public SqlParameter[]? GetAddressParameters(IFormCollection form, string prefix)
    {
        try
        {
            return
            [
            new SqlParameter("@AddressDetails", form[$"{prefix}Address"].ToString()),
            new SqlParameter("@DistrictId", Convert.ToInt32(form[$"{prefix}District"])),
            new SqlParameter("@TehsilId", Convert.ToInt32(form[$"{prefix}Tehsil"])),
            new SqlParameter("@BlockId", Convert.ToInt32(form[$"{prefix}Block"])),
            new SqlParameter("@HalqaPanchayatName", form[$"{prefix}PanchayatMuncipality"].ToString()),
            new SqlParameter("@VillageName", form[$"{prefix}Village"].ToString()),
            new SqlParameter("@WardName", form[$"{prefix}Ward"].ToString()),
            new SqlParameter("@Pincode", form[$"{prefix}Pincode"].ToString())
            ];
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public void UpdateApplication(string columnName, string columnValue, SqlParameter applicationId)
    {
        var columnNameParam = new SqlParameter("@ColumnName", columnName);
        var columnValueParam = new SqlParameter("@ColumnValue", columnValue);

        dbcontext.Database.ExecuteSqlRaw("EXEC UpdateApplication @ColumnName,@ColumnValue,@ApplicationId", columnNameParam, columnValueParam, applicationId);
    }

    public void UpdateApplicationHistory(string applicationId, string actionTaker, string actionTaken, string remarks, string updateObject = "", string File = "")
    {
        // Search for an existing history record
        var historyRecord = dbcontext.ApplicationsHistories.FirstOrDefault(u => u.ApplicationId == applicationId);

        var newAction = new
        {
            ActionTaker = actionTaker,
            ActionTaken = actionTaken,
            Remarks = remarks,
            DateTime = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
            UpdateObject = JsonConvert.DeserializeObject<dynamic>(updateObject),
            File
        };

        if (historyRecord == null)
        {
            // No result was returned, insert a new record with the initial history action
            var newHistory = new ApplicationsHistory
            {
                ApplicationId = applicationId,
                History = JsonConvert.SerializeObject(new List<object> { newAction })
            };

            // Add the new record to the database
            dbcontext.ApplicationsHistories.Add(newHistory);
            dbcontext.SaveChanges();

        }
        else
        {
            // Result was found, update the History property
            var history = JsonConvert.DeserializeObject<List<object>>(historyRecord.History) ?? new List<object>();

            // Add the new action to the history
            history.Add(newAction);

            // Serialize the updated history back to JSON
            historyRecord.History = JsonConvert.SerializeObject(history);

            // Save the changes to the database
            dbcontext.SaveChanges();

        }
    }


    public User GetOfficerDetails(string Designation, string AccessLevel, int AccessCode)
    {
        var officer = dbcontext.Users
        .AsEnumerable()
        .FirstOrDefault(x =>
        {
            var details = JsonConvert.DeserializeObject<dynamic>(x.UserSpecificDetails);
            return details?.Designation == Designation
                && details?.AccessLevel == AccessLevel
                && details?.AccessCode == AccessCode;
        });



        return officer!;
    }
    public (Application UserDetails, AddressJoin PreAddressDetails, AddressJoin PerAddressDetails, dynamic ServiceSpecific, dynamic BankDetails) GetUserDetailsAndRelatedData(string applicationId)
    {
        var userDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId);

        if (userDetails == null)
        {
            throw new Exception("User details not found");
        }

        var preAddressDetails = dbcontext.Set<AddressJoin>()
            .FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", userDetails.PresentAddressId))
            .ToList()
            .FirstOrDefault();

        var perAddressDetails = dbcontext.Set<AddressJoin>()
            .FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", userDetails.PermanentAddressId))
            .ToList()
            .FirstOrDefault();

        var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(userDetails.ServiceSpecific);
        var bankDetails = JsonConvert.DeserializeObject<dynamic>(userDetails.BankDetails);

        return (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails)!;
    }

    public string[] GenerateUniqueRandomCodes(int numberOfCodes, int codeLength)
    {
        HashSet<string> codesSet = new HashSet<string>();
        Random random = new();

        while (codesSet.Count < numberOfCodes)
        {
            const string chars = "0123456789";
            char[] codeChars = new char[codeLength];

            for (int i = 0; i < codeLength; i++)
            {
                codeChars[i] = chars[random.Next(chars.Length)];
            }

            string newCode = new(codeChars);
            codesSet.Add(newCode.ToString());
        }

        string[] codesArray = new string[numberOfCodes];
        codesSet.CopyTo(codesArray);
        return codesArray;
    }


}

