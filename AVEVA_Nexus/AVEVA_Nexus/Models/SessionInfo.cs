namespace AVEVA_Nexus.Models
{
    // Data of GET /api/Session/Info. Fields E3D cannot report are null.
    public class SessionInfo
    {
        public string Project { get; set; }
        public string Mdb { get; set; }
        public string User { get; set; }
        public string Module { get; set; }
        public string E3dVersion { get; set; }
        public bool ReadOnly { get; set; }
    }
}
