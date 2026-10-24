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
                TypeName = t.TypeName,
                IsActive = t.IsActive,
                CourtCount = t.Courts?.Count(c => !c.IsDeleted) ?? 0
            }).ToList();

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
                if (!list.Any(t => t.CourtTypeId == st.Id || t.TypeName.Equals(st.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new CourtTypeDto
                    {
                        CourtTypeId = st.Id,
                        TypeName = st.Name,
                        IsActive = true,
                        CourtCount = 0
                    });
                }
            }

            return list.OrderBy(t => t.CourtTypeId).ToList();
        }
    }
}
