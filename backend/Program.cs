using Microsoft.Data.SqlClient;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Frontend");

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "MGT : Customer Requirement API",
    status = "ok",
    checkedAt = DateTimeOffset.Now
}));

app.MapGet("/api/customer-requirements", async Task<IResult> (
    string? search,
    string? status,
    int? page,
    int? pageSize,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var pageNumber = Math.Max(page ?? 1, 1);
    var take = Math.Clamp(pageSize ?? 50, 1, 200);
    var skip = (pageNumber - 1) * take;
    var normalizedStatus = NormalizeStatus(status);

    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string countSql = """
        WITH DisplayRows AS
        (
            SELECT
                CASE
                    WHEN C.BusinessPartner IS NULL THEN COALESCE(B.NewCode, A.CustomerCode_ECC6)
                    ELSE A.CustomerCode_ECC6
                END AS CustomerCode,
                CASE
                    WHEN C.BusinessPartnerFull IS NULL THEN COALESCE(D.CustomerName, A.CustomerName_ECC6)
                    ELSE C.BusinessPartnerFull
                END AS CustomerName,
                A.CustomerRequirement,
                A.CustomerCode_ECC6,
                A.CustomerName_ECC6,
                A.IsActive
            FROM dbo.Ms_CustomerRequirement AS A
            LEFT JOIN dbo.Setting_MappingData AS B
                ON A.CustomerCode_ECC6 = B.OldCode
            LEFT JOIN dbo.Ms_BusinessPartner AS C
                ON C.BusinessPartner = B.NewCode
            LEFT JOIN dbo.Setting_Mapping_Customer AS D
                ON A.CustomerCode_ECC6 = D.CustomerCode
        )
        SELECT COUNT(1)
        FROM DisplayRows
        WHERE
            (
                @Search IS NULL
                OR CustomerCode LIKE N'%' + @Search + N'%'
                OR CustomerName LIKE N'%' + @Search + N'%'
                OR CustomerCode_ECC6 LIKE N'%' + @Search + N'%'
                OR CustomerName_ECC6 LIKE N'%' + @Search + N'%'
                OR CustomerRequirement LIKE N'%' + @Search + N'%'
            )
            AND
            (
                @Status = N'all'
                OR (@Status = N'active' AND IsActive = 1)
                OR (@Status = N'inactive' AND IsActive = 0)
            );
        """;

    const string listSql = """
        WITH DisplayRows AS
        (
            SELECT
                RTRIM(A.CustomerCode_ECC6) AS CustomerCode_ECC6,
                A.CustomerName_ECC6,
                RTRIM(
                    CASE
                        WHEN C.BusinessPartner IS NULL THEN COALESCE(B.NewCode, A.CustomerCode_ECC6)
                        ELSE A.CustomerCode_ECC6
                    END
                ) AS CustomerCode,
                CASE
                    WHEN C.BusinessPartnerFull IS NULL THEN COALESCE(D.CustomerName, A.CustomerName_ECC6)
                    ELSE C.BusinessPartnerFull
                END AS CustomerName,
                A.CustomerRequirement,
                A.IsActive,
                A.CreatedAt,
                A.CrateedBy,
                A.UpdatedAt,
                A.UpdateBy
            FROM dbo.Ms_CustomerRequirement AS A
            LEFT JOIN dbo.Setting_MappingData AS B
                ON A.CustomerCode_ECC6 = B.OldCode
            LEFT JOIN dbo.Ms_BusinessPartner AS C
                ON C.BusinessPartner = B.NewCode
            LEFT JOIN dbo.Setting_Mapping_Customer AS D
                ON A.CustomerCode_ECC6 = D.CustomerCode
        )
        SELECT
            CustomerCode_ECC6,
            CustomerName_ECC6,
            CustomerCode,
            CustomerName,
            CustomerRequirement,
            IsActive,
            CreatedAt,
            CrateedBy,
            UpdatedAt,
            UpdateBy
        FROM DisplayRows
        WHERE
            (
                @Search IS NULL
                OR CustomerCode LIKE N'%' + @Search + N'%'
                OR CustomerName LIKE N'%' + @Search + N'%'
                OR CustomerCode_ECC6 LIKE N'%' + @Search + N'%'
                OR CustomerName_ECC6 LIKE N'%' + @Search + N'%'
                OR CustomerRequirement LIKE N'%' + @Search + N'%'
            )
            AND
            (
                @Status = N'all'
                OR (@Status = N'active' AND IsActive = 1)
                OR (@Status = N'inactive' AND IsActive = 0)
            )
        ORDER BY CustomerCode_ECC6
        OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
        """;

    var count = await ExecuteScalarAsync<int>(
        connection,
        countSql,
        CreateCommonParameters(search, normalizedStatus),
        cancellationToken);

    var parameters = CreateCommonParameters(search, normalizedStatus);
    parameters.Add(new SqlParameter("@Skip", SqlDbType.Int) { Value = skip });
    parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

    var rows = await QueryAsync(connection, listSql, parameters, cancellationToken);
    return Results.Ok(new CustomerRequirementPage(rows, count, pageNumber, take));
});

app.MapGet("/api/customer-requirements/{customerCode}", async Task<IResult> (
    string customerCode,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = """
        SELECT
            RTRIM(A.CustomerCode_ECC6) AS CustomerCode_ECC6,
            A.CustomerName_ECC6,
            RTRIM(
                CASE
                    WHEN C.BusinessPartner IS NULL THEN COALESCE(B.NewCode, A.CustomerCode_ECC6)
                    ELSE A.CustomerCode_ECC6
                END
            ) AS CustomerCode,
            CASE
                WHEN C.BusinessPartnerFull IS NULL THEN COALESCE(D.CustomerName, A.CustomerName_ECC6)
                ELSE C.BusinessPartnerFull
            END AS CustomerName,
            A.CustomerRequirement,
            A.IsActive,
            A.CreatedAt,
            A.CrateedBy,
            A.UpdatedAt,
            A.UpdateBy
        FROM dbo.Ms_CustomerRequirement AS A
        LEFT JOIN dbo.Setting_MappingData AS B
            ON A.CustomerCode_ECC6 = B.OldCode
        LEFT JOIN dbo.Ms_BusinessPartner AS C
            ON C.BusinessPartner = B.NewCode
        LEFT JOIN dbo.Setting_Mapping_Customer AS D
            ON A.CustomerCode_ECC6 = D.CustomerCode
        WHERE RTRIM(A.CustomerCode_ECC6) = @CustomerCode;
        """;

    var rows = await QueryAsync(
        connection,
        sql,
        [new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 20) { Value = customerCode.Trim() }],
        cancellationToken);

    return rows.Count == 0 ? Results.NotFound() : Results.Ok(rows[0]);
});

app.MapPost("/api/customer-requirements", async Task<IResult> (
    CustomerRequirementRequest request,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var validation = ValidateRequest(request);
    if (validation is not null)
    {
        return validation;
    }

    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = """
        INSERT INTO dbo.Ms_CustomerRequirement
        (
            CustomerCode_ECC6,
            CustomerName_ECC6,
            CustomerRequirement,
            IsActive,
            CreatedAt,
            CrateedBy,
            UpdatedAt,
            UpdateBy
        )
        VALUES
        (
            @CustomerCode,
            @CustomerName,
            @CustomerRequirement,
            @IsActive,
            GETDATE(),
            @UserName,
            NULL,
            NULL
        );
        """;

    try
    {
        await ExecuteNonQueryAsync(connection, sql, RequestParameters(request, "WebApp"), cancellationToken);
        return Results.Created($"/api/customer-requirements/{request.CustomerCode_ECC6.Trim()}", request);
    }
    catch (SqlException exception) when (exception.Number is 2601 or 2627)
    {
        return Results.Conflict(new { message = "Customer code already exists." });
    }
});

app.MapPut("/api/customer-requirements/{customerCode}", async Task<IResult> (
    string customerCode,
    CustomerRequirementRequest request,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(customerCode.Trim(), request.CustomerCode_ECC6.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Customer code in URL and body must match." });
    }

    var validation = ValidateRequest(request);
    if (validation is not null)
    {
        return validation;
    }

    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = """
        UPDATE dbo.Ms_CustomerRequirement
        SET
            CustomerName_ECC6 = @CustomerName,
            CustomerRequirement = @CustomerRequirement,
            IsActive = @IsActive,
            UpdatedAt = GETDATE(),
            UpdateBy = @UserName
        WHERE RTRIM(CustomerCode_ECC6) = @CustomerCode;
        """;

    var affected = await ExecuteNonQueryAsync(connection, sql, RequestParameters(request, "WebApp"), cancellationToken);
    return affected == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapPatch("/api/customer-requirements/{customerCode}/inactive", async Task<IResult> (
    string customerCode,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = """
        UPDATE dbo.Ms_CustomerRequirement
        SET IsActive = 0,
            UpdatedAt = GETDATE(),
            UpdateBy = N'WebApp'
        WHERE RTRIM(CustomerCode_ECC6) = @CustomerCode;
        """;

    var affected = await ExecuteNonQueryAsync(
        connection,
        sql,
        [new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 20) { Value = customerCode.Trim() }],
        cancellationToken);

    return affected == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapPatch("/api/customer-requirements/{customerCode}/active", async Task<IResult> (
    string customerCode,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = """
        UPDATE dbo.Ms_CustomerRequirement
        SET IsActive = 1,
            UpdatedAt = GETDATE(),
            UpdateBy = N'WebApp'
        WHERE RTRIM(CustomerCode_ECC6) = @CustomerCode;
        """;

    var affected = await ExecuteNonQueryAsync(
        connection,
        sql,
        [new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 20) { Value = customerCode.Trim() }],
        cancellationToken);

    return affected == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapDelete("/api/customer-requirements/{customerCode}", async Task<IResult> (
    string customerCode,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    await using var connection = await OpenConnectionAsync(configuration, cancellationToken);

    const string sql = "DELETE FROM dbo.Ms_CustomerRequirement WHERE RTRIM(CustomerCode_ECC6) = @CustomerCode;";
    var affected = await ExecuteNonQueryAsync(
        connection,
        sql,
        [new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 20) { Value = customerCode.Trim() }],
        cancellationToken);

    return affected == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

static string NormalizeStatus(string? status)
{
    return status?.Trim().ToLowerInvariant() switch
    {
        "active" => "active",
        "inactive" => "inactive",
        _ => "all"
    };
}

static List<SqlParameter> CreateCommonParameters(string? search, string status)
{
    return
    [
        new SqlParameter("@Search", SqlDbType.NVarChar, 200)
        {
            Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim()
        },
        new SqlParameter("@Status", SqlDbType.NVarChar, 20) { Value = status }
    ];
}

static IResult? ValidateRequest(CustomerRequirementRequest request)
{
    if (string.IsNullOrWhiteSpace(request.CustomerCode_ECC6))
    {
        return TypedResults.BadRequest(new { message = "Customer code is required." });
    }

    if (request.CustomerCode_ECC6.Trim().Length > 10)
    {
        return TypedResults.BadRequest(new { message = "Customer code must be 10 characters or less." });
    }

    if (request.CustomerName_ECC6?.Length > 200)
    {
        return TypedResults.BadRequest(new { message = "Customer name must be 200 characters or less." });
    }

    return null;
}

static List<SqlParameter> RequestParameters(CustomerRequirementRequest request, string userName)
{
    return
    [
        new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 20) { Value = request.CustomerCode_ECC6.Trim() },
        new SqlParameter("@CustomerName", SqlDbType.NVarChar, 200)
        {
            Value = string.IsNullOrWhiteSpace(request.CustomerName_ECC6) ? DBNull.Value : request.CustomerName_ECC6.Trim()
        },
        new SqlParameter("@CustomerRequirement", SqlDbType.NVarChar, -1)
        {
            Value = string.IsNullOrWhiteSpace(request.CustomerRequirement) ? DBNull.Value : request.CustomerRequirement.Trim()
        },
        new SqlParameter("@IsActive", SqlDbType.Bit) { Value = request.IsActive },
        new SqlParameter("@UserName", SqlDbType.NVarChar, 50) { Value = userName }
    ];
}

static async Task<SqlConnection> OpenConnectionAsync(IConfiguration configuration, CancellationToken cancellationToken)
{
    var connectionString = configuration.GetConnectionString("MgtDatawarehouse");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Missing connection string 'MgtDatawarehouse'. Set ConnectionStrings__MgtDatawarehouse.");
    }

    var connection = new SqlConnection(connectionString);
    await connection.OpenAsync(cancellationToken);
    return connection;
}

static async Task<int> ExecuteNonQueryAsync(
    SqlConnection connection,
    string sql,
    IReadOnlyCollection<SqlParameter> parameters,
    CancellationToken cancellationToken)
{
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddRange(parameters.ToArray());
    return await command.ExecuteNonQueryAsync(cancellationToken);
}

static async Task<T> ExecuteScalarAsync<T>(
    SqlConnection connection,
    string sql,
    IReadOnlyCollection<SqlParameter> parameters,
    CancellationToken cancellationToken)
{
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddRange(parameters.ToArray());
    var value = await command.ExecuteScalarAsync(cancellationToken);
    return (T)Convert.ChangeType(value!, typeof(T));
}

static async Task<List<CustomerRequirementDto>> QueryAsync(
    SqlConnection connection,
    string sql,
    IReadOnlyCollection<SqlParameter> parameters,
    CancellationToken cancellationToken)
{
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddRange(parameters.ToArray());

    var rows = new List<CustomerRequirementDto>();
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        rows.Add(new CustomerRequirementDto(
            reader.GetString("CustomerCode_ECC6"),
            reader.GetNullableString("CustomerName_ECC6"),
            reader.GetNullableString("CustomerCode"),
            reader.GetNullableString("CustomerName"),
            reader.GetNullableString("CustomerRequirement"),
            reader.GetBoolean("IsActive"),
            reader.GetDateTime("CreatedAt"),
            reader.GetNullableString("CrateedBy"),
            reader.GetNullableDateTime("UpdatedAt"),
            reader.GetNullableString("UpdateBy")));
    }

    return rows;
}

public sealed record CustomerRequirementDto(
    string CustomerCode_ECC6,
    string? CustomerName_ECC6,
    string? CustomerCode,
    string? CustomerName,
    string? CustomerRequirement,
    bool IsActive,
    DateTime CreatedAt,
    string? CrateedBy,
    DateTime? UpdatedAt,
    string? UpdateBy);

public sealed record CustomerRequirementRequest(
    string CustomerCode_ECC6,
    string? CustomerName_ECC6,
    string? CustomerRequirement,
    bool IsActive);

public sealed record CustomerRequirementPage(
    IReadOnlyList<CustomerRequirementDto> Items,
    int Total,
    int Page,
    int PageSize);

public static class SqlDataReaderExtensions
{
    public static string GetString(this SqlDataReader reader, string name)
    {
        return reader.GetString(reader.GetOrdinal(name));
    }

    public static bool GetBoolean(this SqlDataReader reader, string name)
    {
        return reader.GetBoolean(reader.GetOrdinal(name));
    }

    public static DateTime GetDateTime(this SqlDataReader reader, string name)
    {
        return reader.GetDateTime(reader.GetOrdinal(name));
    }

    public static string? GetNullableString(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static DateTime? GetNullableDateTime(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
