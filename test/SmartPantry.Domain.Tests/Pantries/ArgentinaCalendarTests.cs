using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Pantries;

public class ArgentinaCalendarTests
{
    [Fact]
    public void Antes_De_Las_3_UTC_En_Argentina_Sigue_Siendo_El_Dia_Anterior()
    {
        // 21/10 02:00 UTC = 20/10 23:00 en Argentina
        var utcNow = new DateTime(2026, 10, 21, 2, 0, 0, DateTimeKind.Utc);

        ArgentinaCalendar.GetToday(utcNow).ShouldBe(new DateOnly(2026, 10, 20));
    }

    [Fact]
    public void Desde_Las_3_UTC_Empieza_El_Dia_En_Argentina()
    {
        // 21/10 03:00 UTC = 21/10 00:00 en Argentina
        var utcNow = new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);

        ArgentinaCalendar.GetToday(utcNow).ShouldBe(new DateOnly(2026, 10, 21));
    }

    [Fact]
    public void Rechaza_Una_Hora_Local()
    {
        Should.Throw<ArgumentException>(() => ArgentinaCalendar.GetToday(DateTime.Now));
    }
}
