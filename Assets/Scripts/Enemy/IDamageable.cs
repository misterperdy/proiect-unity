using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// simple interface for anything that can get hurt
public interface IDamageable
{
    void TakeDamage(int damage);
}