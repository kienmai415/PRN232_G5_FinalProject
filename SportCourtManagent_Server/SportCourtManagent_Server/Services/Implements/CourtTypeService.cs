using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SportCourtManagent_Server.DataAccess.Interfaces;
using SportCourtManagent_Server.DTOs.Court;
using SportCourtManagent_Server.Services.Interfaces;

namespace SportCourtManagent_Server.Services.Implements
{
    public class CourtTypeService : ICourtTypeService
    {
        private readonly ICourtTypeRepository _courtTypeRepo;

        public CourtTypeService(ICourtTypeRepository courtTypeRepo)
        {
            _courtTypeRepo = courtTypeRepo ?? throw new ArgumentNullException(nameof(courtTypeRepo));
        }

        public async Task<IEnumerable<CourtTypeDto>> GetAllActiveAsync()
        {
            var types = await _courtTypeRepo.GetAllActiveAsync();
            var list = types.Select(t => new CourtTypeDto
            {
                CourtTypeId = t.CourtTypeId,
                TypeName = NormalizeTypeName(t.TypeName),
                IsActive = t.IsActive,
                CourtCount = t.Courts?.Count(c => !c.IsDeleted) ?? 0
            }).ToList();

            // Loại bỏ các môn trùng lặp bằng cách nhóm theo tên môn chuẩn hóa
            var distinctList = list
                .GroupBy(t => t.TypeName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CourtTypeDto
                {
                    CourtTypeId = g.First().CourtTypeId,
                    TypeName = g.Key,
                    IsActive = g.Any(x => x.IsActive),
                    CourtCount = g.Sum(x => x.CourtCount)
                })
                .ToList();

            var standardTypes = new (int Id, string Name)[]
            {
                (1, "Cầu lông"),
                (2, "Bóng đá"),
                (3, "Pickleball"),
                (4, "Tennis"),
                (5, "Bóng rổ")
            };

            foreach (var st in standardTypes)
            {
                if (!distinctList.Any(t => t.TypeName.Equals(st.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    distinctList.Add(new CourtTypeDto
                    {
                        CourtTypeId = st.Id,
                        TypeName = st.Name,
                        IsActive = true,
                        CourtCount = 0
                    });
                }
            }

            return distinctList.OrderBy(t => t.CourtTypeId).ToList();
        }

        private static string NormalizeTypeName(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return string.Empty;
            var trimmed = typeName.Trim();
            if (trimmed.Equals("Badminton", StringComparison.OrdinalIgnoreCase)) return "Cầu lông";
            if (trimmed.Equals("Football", StringComparison.OrdinalIgnoreCase)) return "Bóng đá";
            if (trimmed.Equals("Basketball", StringComparison.OrdinalIgnoreCase)) return "Bóng rổ";
            return trimmed;
        }
    }
}
