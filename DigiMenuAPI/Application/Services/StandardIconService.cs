using AutoMapper;
using AutoMapper.QueryableExtensions;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class StandardIconService : IStandardIconService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public StandardIconService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<OperationResult<List<StandardIconReadDto>>> GetAll()
        {
            var icons = await _context.StandardIcons
                .AsNoTracking()
                .ProjectTo<StandardIconReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<StandardIconReadDto>>.Ok(icons);
        }
    }
}