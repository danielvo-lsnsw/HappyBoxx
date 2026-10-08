using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Identity.Api.Infrastructure.Persistence;

internal static class SqlServerErrorCodes
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}