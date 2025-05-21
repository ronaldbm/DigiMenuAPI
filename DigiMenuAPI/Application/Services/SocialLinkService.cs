using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using static DigiMenuAPI.Application.Common.Constants;

namespace DigiMenuAPI.Application.Services
{
    public class SocialLinkService : ISocialLinkService
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<SocialLinkService> logger;

        public SocialLinkService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<SocialLinkService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<List<SocialLinkDto>> GetAll()
        {
            return await context.SocialLink
                                        .ProjectTo<SocialLinkDto>(mapper.ConfigurationProvider)
                                        .ToListAsync();
        }

        public async Task<OperationResult<bool>> Update(List<SocialLinkUpdateDto> socialLinks)
        {
            try
            {
                var linksDbList = await context.SocialLink.ToListAsync();

                var linksDbDictionary = linksDbList.ToDictionary(l => l.Id);

                foreach (var link in socialLinks)
                {
                    if (!linksDbDictionary.TryGetValue(link.Id, out var linkDb))
                    {
                        logger.LogWarning(MessageBuilder.NotFound(EntityNames.SocialLink), link);
                        continue;
                    }

                    linkDb.URL = link.URL;
                    linkDb.IsVisible = link.IsVisible;
                }

                await context.SaveChangesAsync();
                logger.LogUpdate(EntityNames.SocialLink, socialLinks);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.SocialLink));

                return result;

            }
            catch (Exception ex)
            {

                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.SocialLink), socialLinks);

                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.SocialLink));

            }
        }
    }
}
