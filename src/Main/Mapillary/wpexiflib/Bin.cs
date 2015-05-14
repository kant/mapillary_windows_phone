namespace ExifLibrary.Phone
{
    public class Bin
    {
        private string p1;
        private int p2;
        private int p3;
        private object prop;

        public Bin(string p1, int p2, int p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        public Bin(string p1, int p2, int p3, object prop)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.prop = prop;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} - {2} - {3}", p1, p2, p3, prop);
        }
    }
}
