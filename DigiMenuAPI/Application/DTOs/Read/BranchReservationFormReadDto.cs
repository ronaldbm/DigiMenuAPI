namespace DigiMenuAPI.Application.DTOs.Read
{
    public record BranchReservationFormReadDto(
        int Id,
        int BranchId,
        bool FormShowName,
        bool FormRequireName,
        bool FormShowPhone,
        bool FormRequirePhone,
        bool FormShowTable,
        bool FormRequireTable,
        bool FormShowPersons,
        bool FormRequirePersons,
        bool FormShowAllergies,
        bool FormRequireAllergies,
        bool FormShowBirthday,
        bool FormRequireBirthday,
        bool FormShowComments,
        bool FormRequireComments,
        int MaxCapacity,
        int MinutesBeforeClosing,
        int MaxCapacityPerReservation
    );
}