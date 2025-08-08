public partial class Server
{
    public int people_num_now = 0;
    public int people_num_max = 80;

    public int GetPeopleNum()
    {
        return people_num_now;
    }
}