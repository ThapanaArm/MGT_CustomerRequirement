import {
  Edit3,
  List,
  Monitor,
  Plus,
  Printer,
  RefreshCw,
  Save,
  Search,
  Trash2,
  X
} from "lucide-react";
import { FormEvent, useCallback, useEffect, useState, useTransition } from "react";
import {
  createCustomerRequirement,
  deleteCustomerRequirement,
  listCustomerRequirements,
  updateCustomerRequirement
} from "./api";
import type { CustomerRequirement, CustomerRequirementForm, StatusFilter } from "./types";

const emptyForm: CustomerRequirementForm = {
  customerCode_ECC6: "",
  customerName_ECC6: "",
  customerRequirement: "",
  isActive: true
};

type ModalMode = "create" | "edit";

function formatDate(value: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("th-TH", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function displayCode(row: CustomerRequirement) {
  return (row.customerCode ?? row.customerCode_ECC6).trim();
}

function displayName(row: CustomerRequirement) {
  return row.customerName || row.customerName_ECC6 || "-";
}

function requirementLines(row: CustomerRequirement | null) {
  return (row?.customerRequirement ?? "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

function toForm(row: CustomerRequirement): CustomerRequirementForm {
  return {
    customerCode_ECC6: row.customerCode_ECC6.trim(),
    customerName_ECC6: row.customerName_ECC6 ?? "",
    customerRequirement: row.customerRequirement ?? "",
    isActive: row.isActive
  };
}

export default function App() {
  const [rows, setRows] = useState<CustomerRequirement[]>([]);
  const [total, setTotal] = useState(0);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<StatusFilter>("all");
  const [selected, setSelected] = useState<CustomerRequirement | null>(null);
  const [displaySelection, setDisplaySelection] = useState<string | null>(null);
  const [modalMode, setModalMode] = useState<ModalMode | null>(null);
  const [form, setForm] = useState<CustomerRequirementForm>(emptyForm);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [selectedPrintLines, setSelectedPrintLines] = useState<Set<number>>(() => new Set());
  const [isPending, startTransition] = useTransition();

  const loadRows = useCallback(() => {
    setIsLoading(true);
    setError(null);

    listCustomerRequirements({ search, status, page: 1, pageSize: 100 })
      .then((page) => {
        setRows(page.items);
        setTotal(page.total);
      })
      .catch((requestError: Error) => {
        setError(requestError.message);
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [search, status]);

  useEffect(() => {
    const timer = window.setTimeout(loadRows, 220);
    return () => window.clearTimeout(timer);
  }, [loadRows]);

  const currentDisplay = rows.find((row) => row.customerCode_ECC6 === displaySelection) ?? rows[0] ?? null;
  const lines = requirementLines(currentDisplay);

  useEffect(() => {
    setSelectedPrintLines(new Set());
  }, [currentDisplay?.customerCode_ECC6]);

  const openCreate = () => {
    setSelected(null);
    setModalMode("create");
    setForm(emptyForm);
    setNotice(null);
  };

  const openManage = (row: CustomerRequirement) => {
    setSelected(row);
    setDisplaySelection(row.customerCode_ECC6);
    setModalMode("edit");
    setForm(toForm(row));
    setNotice(null);
  };

  const openDisplay = (row: CustomerRequirement) => {
    setDisplaySelection(row.customerCode_ECC6);
  };

  const togglePrintLine = (lineIndex: number) => {
    setSelectedPrintLines((current) => {
      const next = new Set(current);
      if (next.has(lineIndex)) {
        next.delete(lineIndex);
      } else {
        next.add(lineIndex);
      }
      return next;
    });
  };

  const closeModal = () => {
    setSelected(null);
    setModalMode(null);
    setForm(emptyForm);
  };

  const saveForm = (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setNotice(null);

    startTransition(() => {
      const operation = modalMode === "create"
        ? createCustomerRequirement(form)
        : updateCustomerRequirement(form);

      operation
        .then(() => {
          setNotice(modalMode === "create" ? "Customer requirement created." : "Customer requirement updated.");
          setDisplaySelection(form.customerCode_ECC6);
          closeModal();
          loadRows();
        })
        .catch((requestError: Error) => {
          setError(requestError.message);
        });
    });
  };

  const removeRow = (row: CustomerRequirement) => {
    const confirmed = window.confirm(`Delete requirement for ${row.customerCode_ECC6.trim()}?`);
    if (!confirmed) return;

    startTransition(() => {
      deleteCustomerRequirement(row.customerCode_ECC6)
        .then(() => {
          setNotice("Customer requirement deleted.");
          closeModal();
          loadRows();
        })
        .catch((requestError: Error) => {
          setError(requestError.message);
        });
    });
  };

  const printDocument = (row?: CustomerRequirement | null) => {
    if (row) {
      setDisplaySelection(row.customerCode_ECC6);
    }

    window.setTimeout(() => {
      window.print();
    }, 80);
  };

  return (
    <main className="app-shell">
      <header className="window-titlebar">
        <div className="quick-access" aria-hidden="true">
          <span />
          <span />
          <span />
        </div>
        <strong>MGT : Customer Requirement</strong>
        <div className="window-controls" aria-hidden="true">
          <span>_</span>
          <span>□</span>
          <span>×</span>
        </div>
      </header>

      <section className="workspace" id="requirements">
        <section className="content-area">
          <header className="top-bar">
            <div>
              <h1>MGT : Customer Requirement</h1>
            </div>
            <div className="top-actions">
              <button className="secondary-button" type="button" onClick={() => printDocument(currentDisplay)} disabled={!currentDisplay}>
                <Printer size={18} aria-hidden="true" />
                <span>Print</span>
              </button>
              <button className="primary-button" type="button" onClick={openCreate}>
                <Plus size={18} aria-hidden="true" />
                <span>Add Requirement</span>
              </button>
            </div>
          </header>

          <section className="command-row" aria-label="Customer requirement tools">
            <label className="search-box">
              <Search size={18} aria-hidden="true" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search Customer Requirement (Ctrl + F)"
              />
            </label>

            <div className="segmented" aria-label="Status filter">
              {(["all", "active", "inactive"] as StatusFilter[]).map((option) => (
                <button
                  className={status === option ? "selected" : ""}
                  key={option}
                  type="button"
                  onClick={() => setStatus(option)}
                >
                  {option === "all" ? "All" : option === "active" ? "Active" : "Inactive"}
                </button>
              ))}
            </div>

            <button className="icon-button" type="button" onClick={loadRows} aria-label="Refresh">
              <RefreshCw size={18} aria-hidden="true" />
            </button>
          </section>

          {notice ? <div className="notice success">{notice}</div> : null}
          {error ? (
            <div className="notice error">
              <strong>API request failed.</strong>
              <span>{error}</span>
            </div>
          ) : null}

          <DisplayRequirementView
            currentDisplay={currentDisplay}
            formatDate={formatDate}
            isLoading={isLoading}
            lines={lines}
            onDelete={removeRow}
            onDisplay={openDisplay}
            onManage={openManage}
            onPrint={printDocument}
            rows={rows}
            selectedCode={currentDisplay?.customerCode_ECC6 ?? null}
            selectedPrintLines={selectedPrintLines}
            onTogglePrintLine={togglePrintLine}
          />
        </section>
      </section>

      <footer className="app-statusbar">
        <strong>RECORDS: {total}</strong>
      </footer>

      {modalMode ? (
        <ManagePopup
          form={form}
          formatDate={formatDate}
          isPending={isPending}
          mode={modalMode}
          onClose={closeModal}
          onFormChange={setForm}
          onSave={saveForm}
          selected={selected}
        />
      ) : null}
    </main>
  );
}

function DisplayRequirementView({
  currentDisplay,
  formatDate,
  isLoading,
  lines,
  onDelete,
  onDisplay,
  onManage,
  onPrint,
  onTogglePrintLine,
  rows,
  selectedCode,
  selectedPrintLines
}: {
  currentDisplay: CustomerRequirement | null;
  formatDate: (value: string | null) => string;
  isLoading: boolean;
  lines: string[];
  onDelete: (row: CustomerRequirement) => void;
  onDisplay: (row: CustomerRequirement) => void;
  onManage: (row: CustomerRequirement) => void;
  onPrint: (row?: CustomerRequirement | null) => void;
  onTogglePrintLine: (lineIndex: number) => void;
  rows: CustomerRequirement[];
  selectedCode: string | null;
  selectedPrintLines: Set<number>;
}) {
  const hasPrintSelection = selectedPrintLines.size > 0;

  return (
    <section className="display-layout">
      <section className="table-panel display-table-panel">
        <div className="table-header">
          <div>
            <h2>List Customer</h2>
            <p>{isLoading ? "Loading data..." : `${rows.length} records loaded`}</p>
          </div>
          <List size={18} aria-hidden="true" />
        </div>
        <div className="table-scroll display-scroll">
          <table>
            <thead>
              <tr>
                <th>Customer Code</th>
                <th>Customer Name</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 6 }).map((_, index) => (
                  <tr className="skeleton-row" key={index}>
                    <td colSpan={2}><span /></td>
                  </tr>
                ))
              ) : rows.length === 0 ? (
                <tr><td className="empty-state" colSpan={2}>No customer requirements found.</td></tr>
              ) : (
                rows.map((row) => (
                  <tr
                    className={selectedCode === row.customerCode_ECC6 ? "is-selected selectable-row" : "selectable-row"}
                    key={row.customerCode_ECC6}
                    onClick={() => onDisplay(row)}
                  >
                    <td className="code-cell">{displayCode(row)}</td>
                    <td>{displayName(row)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <div className="sap-display-panel print-surface">
        <div className="sap-title">
          <div className="sap-title-main">
            <Monitor size={18} aria-hidden="true" />
            <span>Display Customer Requirement</span>
          </div>
          <div className="sap-title-actions">
            <button className="icon-button" type="button" onClick={() => currentDisplay ? onManage(currentDisplay) : undefined} aria-label="Manage requirement" disabled={!currentDisplay}>
              <Edit3 size={16} aria-hidden="true" />
            </button>
            <button className="icon-button" type="button" onClick={() => onPrint(currentDisplay)} aria-label="Print requirement" disabled={!currentDisplay}>
              <Printer size={16} aria-hidden="true" />
            </button>
            <button className="icon-button danger" type="button" onClick={() => currentDisplay ? onDelete(currentDisplay) : undefined} aria-label="Delete" disabled={!currentDisplay}>
              <Trash2 size={16} aria-hidden="true" />
            </button>
          </div>
        </div>
        <div className="sap-customer-strip">
          <div>
            <span>CustomerCode</span>
            <strong>{currentDisplay ? displayCode(currentDisplay) : "-"}</strong>
          </div>
          <div>
            <span>CustomerName</span>
            <strong>{currentDisplay ? displayName(currentDisplay) : "-"}</strong>
          </div>
          <div>
            <span>Status</span>
            <strong>{currentDisplay?.isActive ? "Active" : currentDisplay ? "Inactive" : "-"}</strong>
          </div>
        </div>
        <div className="print-meta">
          <span>Printed from MGT : Customer Requirement</span>
          <span>Updated: {formatDate(currentDisplay?.updatedAt ?? null)}</span>
        </div>
        <div className="sap-grid">
          <div className="sap-grid-head">
            <span className="print-check-header" aria-hidden="true" />
            <span>Text</span>
          </div>
          {lines.length > 0 ? (
            lines.map((line, index) => (
              <div
                className={hasPrintSelection && !selectedPrintLines.has(index) ? "sap-grid-row print-excluded" : "sap-grid-row"}
                key={`${line}-${index}`}
              >
                <label className="line-print-check" aria-label={`Print line ${index + 1}`}>
                  <input
                    type="checkbox"
                    checked={selectedPrintLines.has(index)}
                    onChange={() => onTogglePrintLine(index)}
                  />
                </label>
                <p>{line}</p>
              </div>
            ))
          ) : (
            <div className="sap-empty">No customer requirement text.</div>
          )}
        </div>
      </div>
    </section>
  );
}

function ManagePopup({
  form,
  formatDate,
  isPending,
  mode,
  onClose,
  onFormChange,
  onSave,
  selected
}: {
  form: CustomerRequirementForm;
  formatDate: (value: string | null) => string;
  isPending: boolean;
  mode: ModalMode;
  onClose: () => void;
  onFormChange: (form: CustomerRequirementForm) => void;
  onSave: (event: FormEvent) => void;
  selected: CustomerRequirement | null;
}) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="manage-modal" role="dialog" aria-modal="true" aria-labelledby="manage-title">
        <div className="drawer-header">
          <div>
            <span>{mode === "create" ? "New record" : "Manage Customer Requirement"}</span>
            <h2 id="manage-title">{form.customerCode_ECC6 || "Customer requirement"}</h2>
          </div>
          <button className="icon-button" type="button" onClick={onClose} aria-label="Close popup">
            <X size={18} aria-hidden="true" />
          </button>
        </div>

        <form className="drawer-form" onSubmit={onSave}>
          <label>
            <span>CustomerCode_ECC6</span>
            <input
              value={form.customerCode_ECC6}
              disabled={mode !== "create"}
              maxLength={10}
              onChange={(event) => onFormChange({ ...form, customerCode_ECC6: event.target.value })}
              required
            />
          </label>

          <label>
            <span>CustomerName_ECC6</span>
            <input
              value={form.customerName_ECC6}
              maxLength={200}
              onChange={(event) => onFormChange({ ...form, customerName_ECC6: event.target.value })}
            />
          </label>

          <label>
            <span>CustomerRequirement</span>
            <textarea
              value={form.customerRequirement}
              rows={10}
              onChange={(event) => onFormChange({ ...form, customerRequirement: event.target.value })}
            />
          </label>

          <label className="toggle-row">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => onFormChange({ ...form, isActive: event.target.checked })}
            />
            <span>IsActive</span>
          </label>

          {selected ? (
            <div className="meta-grid">
              <div>
                <span>CreatedAt</span>
                <strong>{formatDate(selected.createdAt)}</strong>
              </div>
              <div>
                <span>CrateedBy</span>
                <strong>{selected.crateedBy || "-"}</strong>
              </div>
              <div>
                <span>UpdatedAt</span>
                <strong>{formatDate(selected.updatedAt)}</strong>
              </div>
              <div>
                <span>UpdateBy</span>
                <strong>{selected.updateBy || "-"}</strong>
              </div>
            </div>
          ) : null}

          <div className="drawer-actions">
            <button className="secondary-button" type="button" onClick={onClose}>
              <X size={17} aria-hidden="true" />
              <span>Cancel</span>
            </button>
            <button className="primary-button" type="submit" disabled={isPending}>
              <Save size={17} aria-hidden="true" />
              <span>{isPending ? "Saving..." : "Save"}</span>
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
