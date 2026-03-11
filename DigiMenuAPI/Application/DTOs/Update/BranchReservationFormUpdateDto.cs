namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchReservationFormUpdateDto(
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