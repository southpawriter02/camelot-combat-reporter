/**
 * TransactionManager - Manages database transactions
 */
import type { Transaction } from '../types.js';
import type { DatabaseAdapter } from '../adapters/DatabaseAdapter.js';
import { TransactionError } from '../errors.js';

/**
 * Transaction state
 */
export type TransactionState = 'active' | 'committed' | 'rolled_back';

/**
 * Managed transaction with state tracking
 */
export interface ManagedTransaction {
  /** The underlying transaction */
  transaction: Transaction;
  /** Current state */
  state: TransactionState;
  /** When the transaction was started */
  startedAt: Date;
  /** Savepoint names */
  savepoints: string[];
}

/**
 * TransactionManager provides utilities for managing database transactions
 */
export class TransactionManager {
  private db: DatabaseAdapter;
  private activeTransactions = new Map<string, ManagedTransaction>();

  constructor(db: DatabaseAdapter) {
    this.db = db;
  }

  /**
   * Begin a new managed transaction
   */
  async begin(): Promise<ManagedTransaction> {
    const tx = await this.db.beginTransaction();
    const managed: ManagedTransaction = {
      transaction: tx,
      state: 'active',
      startedAt: new Date(),
      savepoints: [],
    };
    this.activeTransactions.set(tx.id, managed);
    return managed;
  }

  /**
   * Commit a managed transaction
   */
  async commit(managed: ManagedTransaction): Promise<void> {
    if (managed.state !== 'active') {
      throw new TransactionError(
        `Cannot commit: transaction is ${managed.state}`,
        managed.transaction.id
      );
    }

    await managed.transaction.commit();
    managed.state = 'committed';
    this.activeTransactions.delete(managed.transaction.id);
  }

  /**
   * Rollback a managed transaction
   */
  async rollback(managed: ManagedTransaction): Promise<void> {
    if (managed.state !== 'active') {
      return; // Already completed
    }

    await managed.transaction.rollback();
    managed.state = 'rolled_back';
    this.activeTransactions.delete(managed.transaction.id);
  }

  /**
   * Execute a function within a transaction with automatic commit/rollback
   */
  async executeInTransaction<T>(
    fn: (tx: Transaction) => Promise<T>
  ): Promise<T> {
    const managed = await this.begin();

    try {
      const result = await fn(managed.transaction);
      await this.commit(managed);
      return result;
    } catch (error) {
      await this.rollback(managed);
      throw error;
    }
  }

  /**
   * Execute multiple operations in a single transaction
   */
  async executeAll<T>(
    operations: Array<(tx: Transaction) => Promise<T>>
  ): Promise<T[]> {
    return this.executeInTransaction(async (tx) => {
      const results: T[] = [];
      for (const op of operations) {
        results.push(await op(tx));
      }
      return results;
    });
  }

  /**
   * Get number of active transactions
   */
  getActiveCount(): number {
    return this.activeTransactions.size;
  }

  /**
   * Get all active transactions
   */
  getActiveTransactions(): ManagedTransaction[] {
    return Array.from(this.activeTransactions.values());
  }

  /**
   * Rollback all active transactions (for cleanup)
   */
  async rollbackAll(): Promise<void> {
    const transactions = this.getActiveTransactions();
    for (const managed of transactions) {
      try {
        await this.rollback(managed);
      } catch {
        // Ignore errors during cleanup
      }
    }
  }

  /**
   * Check if a transaction is still active
   */
  isActive(transactionId: string): boolean {
    const managed = this.activeTransactions.get(transactionId);
    return managed?.state === 'active';
  }

  /**
   * Get transaction by ID
   */
  getTransaction(transactionId: string): ManagedTransaction | undefined {
    return this.activeTransactions.get(transactionId);
  }
}
