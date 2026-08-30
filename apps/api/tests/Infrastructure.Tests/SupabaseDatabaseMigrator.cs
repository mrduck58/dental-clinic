using Npgsql;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests;

[TestFixture]
public class SupabaseDatabaseMigrator
{
    private static readonly string[] ConnectionStrings =
    [
        "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.iyuwmzlolzsdqcucgufr;Password=Huan0508@2004;SslMode=Require;TrustServerCertificate=true;Timeout=10;CommandTimeout=30;",
        "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.iyuwmzlolzsdqcucgufr;Password=Huan0508@2004;SslMode=Require;TrustServerCertificate=true;Timeout=10;CommandTimeout=30;",
        "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.iyuwmzlolzsdqcucgufr;Password=Huan0508@2004;SslMode=Require;TrustServerCertificate=true;Timeout=10;CommandTimeout=30;",
        "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.iyuwmzlolzsdqcucgufr;Password=Huan0508@2004;SslMode=Require;TrustServerCertificate=true;Timeout=10;CommandTimeout=30;"
    ];

    private static async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        foreach (var cs in ConnectionStrings)
        {
            try
            {
                var conn = new NpgsqlConnection(cs);
                await conn.OpenAsync();
                return conn;
            }
            catch
            {
                // try next
            }
        }
        throw new InvalidOperationException("Could not connect to Supabase with any connection string.");
    }

    [Test, Explicit("Chạy trực tiếp khi cần migrate database Supabase")]
    public async Task ApplySchemaUpdatesToSupabase()
    {
        var sqlCommands = new[]
        {
            // Appointments columns
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AppointmentType"" character varying(50) NOT NULL DEFAULT 'GeneralExam';",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpId"" uuid NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpFromAppointmentId"" uuid NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpDate"" timestamp with time zone NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpNote"" text NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummary"" text NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummaryGeneratedAt"" timestamp with time zone NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummaryBasedOnCount"" integer NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""QueueEntryOrder"" bigint NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""QueueOrder"" bigint NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CheckedInAt"" timestamp with time zone NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancellationReason"" character varying(50) NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancellationNote"" character varying(500) NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""RescheduledCount"" integer NOT NULL DEFAULT 0;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""LastRescheduledAt"" timestamp with time zone NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancelledAt"" timestamp with time zone NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancelledByUserId"" uuid NULL;",
            @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""Origin"" character varying(20) NOT NULL DEFAULT 'Online';",

            // Diagnoses
            @"CREATE TABLE IF NOT EXISTS ""Diagnoses"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""Description"" character varying(1000) NOT NULL,
                ""MedicalHistory"" character varying(2000) NULL,
                ""AllergyHistory"" character varying(2000) NULL,
                ""Conclusion"" character varying(2000) NULL,
                ""GumCondition"" character varying(500) NULL,
                ""OralMucosaCondition"" character varying(500) NULL,
                ""GumBleeding"" character varying(500) NULL,
                ""PainOnChewing"" character varying(500) NULL,
                ""TeethCount"" character varying(500) NULL,
                ""DecayedTeeth"" character varying(500) NULL,
                ""WornOrBrokenTeeth"" character varying(500) NULL,
                ""LooseTeeth"" character varying(500) NULL,
                ""Tartar"" character varying(500) NULL,
                ""Plaque"" character varying(500) NULL,
                ""BadBreath"" character varying(500) NULL,
                ""TmjSymptoms"" character varying(500) NULL,
                ""Occlusion"" character varying(500) NULL,
                ""OcclusionDeviation"" character varying(500) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_Diagnoses_AppointmentId"" ON ""Diagnoses"" (""AppointmentId"");",

            // TreatmentPlans
            @"CREATE TABLE IF NOT EXISTS ""TreatmentPlans"" (
                ""Id"" uuid PRIMARY KEY,
                ""PatientId"" uuid NOT NULL,
                ""DentistId"" uuid NOT NULL,
                ""AppointmentId"" uuid NULL,
                ""Title"" character varying(200) NOT NULL,
                ""Status"" character varying(50) NOT NULL,
                ""Notes"" character varying(2000) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""CompletedAt"" timestamp with time zone NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlans_PatientId"" ON ""TreatmentPlans"" (""PatientId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlans_AppointmentId"" ON ""TreatmentPlans"" (""AppointmentId"");",

            // TreatmentPlanItems
            @"CREATE TABLE IF NOT EXISTS ""TreatmentPlanItems"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanId"" uuid NOT NULL,
                ""ServiceId"" uuid NOT NULL,
                ""ServiceOptionId"" uuid NULL,
                ""ServiceOptionName"" character varying(200) NULL,
                ""UnitPrice"" numeric(18, 2) NOT NULL,
                ""Quantity"" integer NOT NULL,
                ""Teeth"" character varying(200) NULL,
                ""Status"" character varying(50) NOT NULL,
                ""WarrantyUntil"" date NULL,
                ""Notes"" character varying(2000) NULL,
                ""EstimatedSessionCount"" integer NULL,
                ""EstimatedDurationMin"" integer NULL,
                ""EstimatedDurationMax"" integer NULL,
                ""EstimatedDurationUnit"" character varying(20) NULL,
                ""EstimatedStartDate"" date NULL,
                ""EstimatedEndDate"" date NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""CompletedAt"" timestamp with time zone NULL
            );",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedSessionCount"" integer NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMin"" integer NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMax"" integer NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationUnit"" character varying(20) NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedStartDate"" date NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedEndDate"" date NULL;",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlanItems_TreatmentPlanId"" ON ""TreatmentPlanItems"" (""TreatmentPlanId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlanItems_ServiceId"" ON ""TreatmentPlanItems"" (""ServiceId"");",

            // TreatmentProcedures
            @"CREATE TABLE IF NOT EXISTS ""TreatmentProcedures"" (
                ""Id"" uuid PRIMARY KEY,
                ""ServiceId"" uuid NOT NULL,
                ""StepNumber"" integer NOT NULL,
                ""Name"" character varying(300) NOT NULL,
                ""DurationMinutes"" integer NOT NULL DEFAULT 30,
                ""IsRequired"" boolean NOT NULL DEFAULT true,
                ""Description"" text NULL
            );",
            @"ALTER TABLE ""TreatmentProcedures"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;",
            @"ALTER TABLE ""TreatmentProcedures"" ADD COLUMN IF NOT EXISTS ""IsRequired"" boolean NOT NULL DEFAULT true;",
            @"ALTER TABLE ""TreatmentProcedures"" ADD COLUMN IF NOT EXISTS ""Description"" text NULL;",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentProcedures_ServiceId"" ON ""TreatmentProcedures"" (""ServiceId"");",

            // TreatmentSessions
            @"CREATE TABLE IF NOT EXISTS ""TreatmentSessions"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanItemId"" uuid NOT NULL,
                ""TreatmentProcedureId"" uuid NULL,
                ""DentistId"" uuid NULL,
                ""SessionNumber"" integer NOT NULL DEFAULT 1,
                ""Name"" character varying(200) NOT NULL,
                ""Status"" character varying(50) NOT NULL,
                ""DurationMinutes"" integer NOT NULL DEFAULT 30,
                ""Percent"" integer NOT NULL DEFAULT 0,
                ""PerformedAt"" timestamp with time zone NULL,
                ""Note"" character varying(2000) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );",
            @"ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;",
            @"ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""Percent"" integer NOT NULL DEFAULT 0;",
            @"ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""SessionNumber"" integer NOT NULL DEFAULT 1;",
            @"ALTER TABLE ""Diagnoses"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp with time zone NULL;",
            @"ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""TotalCost"" numeric(18, 2) NOT NULL DEFAULT 0;",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentSessions_TreatmentPlanItemId"" ON ""TreatmentSessions"" (""TreatmentPlanItemId"");",

            // AppointmentSessions
            @"CREATE TABLE IF NOT EXISTS ""AppointmentSessions"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""TreatmentSessionId"" uuid NOT NULL,
                ""Note"" character varying(1000) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_AppointmentSessions_AppointmentId"" ON ""AppointmentSessions"" (""AppointmentId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_AppointmentSessions_TreatmentSessionId"" ON ""AppointmentSessions"" (""TreatmentSessionId"");",

            // FollowUps
            @"CREATE TABLE IF NOT EXISTS ""FollowUps"" (
                ""Id"" uuid PRIMARY KEY,
                ""PatientId"" uuid NOT NULL,
                ""DentistId"" uuid NOT NULL,
                ""OriginAppointmentId"" uuid NOT NULL,
                ""TreatmentPlanItemId"" uuid NULL,
                ""TreatmentSessionId"" uuid NULL,
                ""AppointmentId"" uuid NULL,
                ""DueDate"" timestamp with time zone NOT NULL,
                ""Note"" character varying(2000) NULL,
                ""Status"" character varying(50) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""CompletedAt"" timestamp with time zone NULL,
                ""CancelledAt"" timestamp with time zone NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_FollowUps_PatientId"" ON ""FollowUps"" (""PatientId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_FollowUps_DueDate"" ON ""FollowUps"" (""DueDate"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_FollowUps_Status"" ON ""FollowUps"" (""Status"");",

            // Prescriptions
            @"CREATE TABLE IF NOT EXISTS ""Prescriptions"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""Notes"" character varying(2000) NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_Prescriptions_AppointmentId"" ON ""Prescriptions"" (""AppointmentId"");",

            // PrescriptionItems
            @"CREATE TABLE IF NOT EXISTS ""PrescriptionItems"" (
                ""Id"" uuid PRIMARY KEY,
                ""PrescriptionId"" uuid NOT NULL,
                ""MedicineName"" character varying(200) NOT NULL,
                ""Dosage"" character varying(50) NOT NULL,
                ""Quantity"" integer NOT NULL,
                ""Unit"" character varying(20) NOT NULL,
                ""Usage"" character varying(500) NOT NULL,
                ""Notes"" character varying(500) NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_PrescriptionItems_PrescriptionId"" ON ""PrescriptionItems"" (""PrescriptionId"");",

            // AppointmentPhotos
            @"CREATE TABLE IF NOT EXISTS ""AppointmentPhotos"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""Section"" character varying(30) NOT NULL,
                ""Url"" character varying(1000) NOT NULL,
                ""Note"" character varying(1000) NULL,
                ""UploadedBy"" character varying(200) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_AppointmentPhotos_AppointmentId_Section"" ON ""AppointmentPhotos"" (""AppointmentId"", ""Section"");",

            // TreatmentSupplyUsages
            @"CREATE TABLE IF NOT EXISTS ""TreatmentSupplyUsages"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanId"" uuid NULL,
                ""TreatmentSessionId"" uuid NULL,
                ""SupplyItemId"" uuid NOT NULL,
                ""SupplyTransactionId"" uuid NULL,
                ""StepEntryId"" uuid NULL,
                ""Quantity"" integer NOT NULL,
                ""UnitCostAtUsage"" numeric(18, 2) NOT NULL DEFAULT 0,
                ""CreatedBy"" character varying(200) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentSupplyUsages_TreatmentPlanId"" ON ""TreatmentSupplyUsages"" (""TreatmentPlanId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_TreatmentSupplyUsages_TreatmentSessionId"" ON ""TreatmentSupplyUsages"" (""TreatmentSessionId"");",

            // MaterialRequests & MaterialRequestItems
            @"CREATE TABLE IF NOT EXISTS ""MaterialRequests"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NULL,
                ""DentistId"" uuid NOT NULL,
                ""RequestedByUserId"" uuid NOT NULL,
                ""Title"" character varying(200) NOT NULL,
                ""Status"" character varying(50) NOT NULL,
                ""Note"" text NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""ApprovedAt"" timestamp with time zone NULL
            );",
            @"CREATE TABLE IF NOT EXISTS ""MaterialRequestItems"" (
                ""Id"" uuid PRIMARY KEY,
                ""MaterialRequestId"" uuid NOT NULL,
                ""SupplyItemId"" uuid NOT NULL,
                ""Quantity"" integer NOT NULL,
                ""Note"" text NULL
            );"
        };

        await using var conn = await GetOpenConnectionAsync();

        foreach (var sql in sqlCommands)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
                TestContext.WriteLine($"Executed successfully: {sql.Split('\n')[0]}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Warning executing {sql.Split('\n')[0]}: {ex.Message}");
            }
        }
    }

    [Test, Explicit("Chạy trực tiếp khi cần kiểm tra toàn diện schema trên Supabase")]
    public async Task CheckAllTablesAndColumnsOnSupabase()
    {
        await using var conn = await GetOpenConnectionAsync();

        // 1. Get all public tables
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // 2. Get all public columns
        var tableColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new NpgsqlCommand("SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var tbl = reader.GetString(0);
                var col = reader.GetString(1);
                if (!tableColumns.TryGetValue(tbl, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    tableColumns[tbl] = set;
                }
                set.Add(col);
            }
        }

        // 3. Expected core tables and key columns for clinical & appointment workflow
        var expectedSchema = new Dictionary<string, string[]>
        {
            ["Users"] = ["Id", "Email", "FullName", "Role", "IsActive", "PhoneNumber", "Gender"],
            ["Patients"] = ["Id", "UserId", "DateOfBirth", "MedicalHistory", "PrimaryPatientId", "Relationship"],
            ["Employees"] = ["Id", "UserId", "EmployeeCode"],
            ["DentistProfiles"] = ["Id", "EmployeeId", "Specialization", "LicenseNumber", "Shift", "ExperienceYears"],
            ["Appointments"] = [
                "Id", "PatientId", "DentistId", "ServiceId", "AppointmentDate", "Status",
                "AppointmentType", "DurationMinutes", "Origin", "Notes", "Symptoms", "CheckedInAt",
                "AiSummary", "AiSummaryGeneratedAt", "AiSummaryBasedOnCount", "FollowUpId",
                "FollowUpFromAppointmentId", "FollowUpDate", "FollowUpNote", "CancellationReason",
                "CancellationNote", "RescheduledCount", "LastRescheduledAt", "CancelledAt", "CancelledByUserId"
            ],
            ["Diagnoses"] = [
                "Id", "AppointmentId", "Description", "MedicalHistory", "AllergyHistory", "Conclusion",
                "GumCondition", "OralMucosaCondition", "GumBleeding", "PainOnChewing", "TeethCount",
                "DecayedTeeth", "WornOrBrokenTeeth", "LooseTeeth", "Tartar", "Plaque", "BadBreath"
            ],
            ["TreatmentPlans"] = ["Id", "PatientId", "DentistId", "AppointmentId", "Title", "Status", "Notes", "CreatedAt", "CompletedAt"],
            ["TreatmentPlanItems"] = [
                "Id", "TreatmentPlanId", "ServiceId", "UnitPrice", "Quantity", "Teeth", "Status", "Notes",
                "EstimatedSessionCount", "EstimatedDurationMin", "EstimatedDurationMax", "EstimatedDurationUnit",
                "EstimatedStartDate", "EstimatedEndDate"
            ],
            ["TreatmentProcedures"] = ["Id", "ServiceId", "StepNumber", "Name", "DurationMinutes", "IsRequired"],
            ["TreatmentSessions"] = ["Id", "TreatmentPlanItemId", "DentistId", "StepOrder", "Name", "Status", "EstimatedDurationMinutes"],
            ["AppointmentSessions"] = ["Id", "AppointmentId", "TreatmentSessionId", "Note", "CreatedAt"],
            ["FollowUps"] = ["Id", "PatientId", "DentistId", "OriginAppointmentId", "DueDate", "Status", "Note", "CreatedAt"],
            ["Prescriptions"] = ["Id", "AppointmentId", "CreatedAt", "Notes"],
            ["PrescriptionItems"] = ["Id", "PrescriptionId", "MedicineName", "Dosage", "Quantity", "Unit", "Usage"],
            ["AppointmentPhotos"] = ["Id", "AppointmentId", "Section", "Url", "UploadedBy", "CreatedAt"],
            ["TreatmentSupplyUsages"] = ["Id", "TreatmentPlanId", "TreatmentSessionId", "SupplyItemId", "Quantity", "UnitCostAtUsage"],
            ["MaterialRequests"] = ["Id", "AppointmentId", "DentistId", "RequestedByUserId", "Title", "Status"],
            ["MaterialRequestItems"] = ["Id", "MaterialRequestId", "SupplyItemId", "Quantity"],
            ["Services"] = ["Id", "Name", "Price", "Description"],
            ["ServiceOptions"] = ["Id", "ServiceId", "Name", "Price"],
            ["Invoices"] = ["Id", "AppointmentId", "PatientId", "TotalAmount", "Status"],
            ["InvoiceItems"] = ["Id", "InvoiceId", "ItemName", "UnitPrice", "Quantity", "TotalPrice"],
            ["PaymentTransactions"] = ["Id", "InvoiceId", "Amount", "Status", "PaymentMethod"],
            ["WorkSchedules"] = ["Id", "Date", "Shift", "StaffName", "Room"],
            ["UserDeviceTokens"] = ["Id", "UserId", "Token"],
            ["AppointmentSlotHolds"] = ["Id", "PatientId", "DentistId", "AppointmentDate", "TimeSlot", "ExpiresAt", "Status", "DurationMinutes"],
            ["AppointmentChangeRequests"] = ["Id", "AppointmentId", "PatientId", "RequestedByUserId", "Type", "Status", "Reason"]
        };

        var missingTables = new List<string>();
        var missingColumns = new List<string>();

        TestContext.WriteLine("=== SUPABASE DATABASE SCHEMA VERIFICATION REPORT ===");
        TestContext.WriteLine($"Total tables in public schema: {tables.Count}");
        TestContext.WriteLine("-----------------------------------------------------");

        foreach (var (tbl, cols) in expectedSchema)
        {
            if (!tables.Contains(tbl))
            {
                TestContext.WriteLine($"[MISSING TABLE] {tbl}");
                missingTables.Add(tbl);
                continue;
            }

            var actualCols = tableColumns.GetValueOrDefault(tbl, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            var missingForTable = cols.Where(c => !actualCols.Contains(c)).ToList();

            if (missingForTable.Count > 0)
            {
                TestContext.WriteLine($"[PARTIAL TABLE] {tbl}: Missing columns -> {string.Join(", ", missingForTable)}");
                missingColumns.AddRange(missingForTable.Select(c => $"{tbl}.{c}"));
            }
            else
            {
                TestContext.WriteLine($"[OK] {tbl} ({actualCols.Count} columns verified)");
            }
        }

        TestContext.WriteLine("-----------------------------------------------------");
        TestContext.WriteLine($"Result: {missingTables.Count} missing tables, {missingColumns.Count} missing columns.");

        Assert.That(missingTables, Is.Empty, $"Missing tables on Supabase: {string.Join(", ", missingTables)}");
        Assert.That(missingColumns, Is.Empty, $"Missing columns on Supabase: {string.Join(", ", missingColumns)}");
    }
}
