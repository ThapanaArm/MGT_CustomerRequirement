export type StatusFilter = "all" | "active" | "inactive";

export interface CustomerRequirement {
  customerCode_ECC6: string;
  customerName_ECC6: string | null;
  customerCode: string | null;
  customerName: string | null;
  customerRequirement: string | null;
  isActive: boolean;
  createdAt: string;
  crateedBy: string | null;
  updatedAt: string | null;
  updateBy: string | null;
}

export interface CustomerRequirementPage {
  items: CustomerRequirement[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CustomerRequirementForm {
  customerCode_ECC6: string;
  customerName_ECC6: string;
  customerRequirement: string;
  isActive: boolean;
}
