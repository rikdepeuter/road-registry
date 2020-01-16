namespace RoadRegistry.BackOffice.Schema.Organizations
{
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public class OrganizationDbaseRecord : DbaseRecord
    {
        public static readonly OrganizationDbaseSchema Schema = new OrganizationDbaseSchema();

        public OrganizationDbaseRecord()
        {
            ORG = new DbaseString(Schema.ORG);
            LBLORG = new DbaseString(Schema.LBLORG);

            Values = new DbaseFieldValue[]
            {
                ORG,
                LBLORG,
            };
        }

        public DbaseString ORG { get; }
        public DbaseString LBLORG { get; }
    }
}
