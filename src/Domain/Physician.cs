using System;

namespace MedicalModel.Domain;

public class Physician
{
public int Id{get;set;}
public double CurrentLoad{get;set;}
public const int MaxLoad = 1;
public bool CanAccept(double attention) =>
       CurrentLoad + attention <= MaxLoad;
}
